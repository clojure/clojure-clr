# Variants

Some places where ClojureCLR differs from ClojureJVM in peculiar ways.
Most of these were discovered working with the new test suite for clojure.core that Jank has been developing.
Most of my examples below come from that test suite.

## Numeric operations

There are some differences between the JVM and CLR in the primitive numeric types. And some differences in the way I have coded some Clojure core functions.

### Byte

`java.lang.Byte` and the underlying primitive are signed.  `System.Byte` is unsigned.  CLR does offer `System.SByte`, a signed byte.  I've never seen `SByte` in real life.  I decided to map Clojure's byte-related operations to `System.Byte` so that the CLR experience would be natural.
This means there is a variation in the operations.

|     | JVM | CLR |
|-----|-----|-----|
| `(byte -128)` | => 128 |  throws a "value out of range" exception   |
| `(byte 128)`  | throws an exception | => 128 |

In ClojureCLR,  the `byte` function on a numeric argument of any type with a negative value will throw an exception; called on argument with range of 128 to 255, it will succeed.

> I have no intention to change this behavior.  This is a question of what datatype the byte-oriented Clojure functions are manipulated.  `System.Byte` was the obvious choice.  Changing this would cause massive breakage.  This really is a platform difference, and coders writing cross-platform need to be aware of it.

## Numeric conversions

ClojureJVM and ClojureCLR differ in the way they handle numeric conversions. Let's look at the `short` function in Clojure core.

```Clojure
(defn short
  "Coerce to short"
  {:inline (fn  [x] `(. clojure.lang.RT (~(if *unchecked-math* 'uncheckedShortCast 'shortCast) ~x)))
   :added "1.0"}
  [x] (. clojure.lang.RT (shortCast x)))
```

This is the same for ClojureJVM and ClojureCLR.  The difference is in the coding for `RT.shortCast`.  For the JVM, this is

```java
static public short shortCast(Object x){
	if(x instanceof Short)
		return ((Short) x).shortValue();
	long n = longCast(x);
	if(n < Short.MIN_VALUE || n > Short.MAX_VALUE)
		throw new IllegalArgumentException("Value out of range for short: " + x);

	return (short) n;
}

static public short shortCast(byte x){
	return x;
}

static public short shortCast(short x){
	return x;
}

// etc. for the remaining primitive numeric types
```

Now, is it turns out, ClojureCLR is missing the overloads for `shortCast`, having only the one taking an `object`.  This is an efficiency matter, and I will fix it.  (For the major conversions, `intCast`, `longCast`, `doubleCast`, and `floatCast`, the overloads are present.)  

The variant of interest here is for the `Object` case.  In ClojureCLR, this is

```C#
public static short shortCast(object x)
{
    if (x is short s)
        return s;
    long n = Util.ConvertToLong(x);
    if (n < short.MinValue || n > short.MaxValue)
        throw new ArgumentException("Value out of range for short: " + x);

    return (short)n;
}
```

The only difference:  the JVM code calls `RT.longCast`; the CLR code calls `Util.ConvertToLong`.  Let's compare.

```java
static public long longCast(Object x){
	if(x instanceof Integer || x instanceof Long)
		return ((Number) x).longValue();
	else if (x instanceof BigInt)
		{
		BigInt bi = (BigInt) x;
		if(bi.bipart == null)
			return bi.lpart;
		else
			throw new IllegalArgumentException("Value out of range for long: " + x);
		}
	else if (x instanceof BigInteger)
		{
		BigInteger bi = (BigInteger) x;
		if(bi.bitLength() < 64)
			return bi.longValue();
		else
			throw new IllegalArgumentException("Value out of range for long: " + x);
		}
	else if (x instanceof Byte || x instanceof Short)
	    return ((Number) x).longValue();
	else if (x instanceof Ratio)
	    return longCast(((Ratio)x).bigIntegerValue());
	else if (x instanceof Character)
	    return longCast(((Character) x).charValue());
	else
	    return longCast(((Number)x).doubleValue());
}
```

ClojureCLR does have a version of `RT.longCast` that tries to match this closely. However, there are a lot more numeric types (all the unsigned types, `decimal`, `SBtye`).  So the default case ends up doing pretty much what `Util.ConvertToLong` does.  At any rate, I decided for `shortCast` to just go directly to `Util.ConvertToLong`:

```C#
public static long ConvertToLong(object o)
{
    switch (Type.GetTypeCode(o.GetType()))
    {
        case TypeCode.Byte:
            return (long)(Byte)o;
        case TypeCode.Char:
            return (long)(Char)o;
        case TypeCode.Decimal:
            return (long)(decimal)o;
        case TypeCode.Double:
            return (long)(double)o;
        case TypeCode.Int16:
            return (long)(short)o;
        case TypeCode.Int32:
            return (long)(int)o;
        case TypeCode.Int64:
            return (long)o;
        case TypeCode.SByte:
            return (long)(sbyte)o;
        case TypeCode.Single:
            return (long)(float)o;
        case TypeCode.UInt16:
            return (long)(ushort)o;
        case TypeCode.UInt32:
            return (long)(uint)o;
        case TypeCode.UInt64:
            return (long)(ulong)o;
        default:
            return Convert.ToInt64(o, CultureInfo.InvariantCulture);
    }
}
```
 The inspiration for this code came from what I found in IronPython and IronScheme.  It relies on the `System.Convert` class to do any conversion not handled by specific cases in the `switch` statement.   ClojureCLR defines these conversions for all the numeric types it implements: `Ratio`, `BigInteger`, `BigDecimal`, and `BigInt`.  (I think that's the whole list.)  We're pretty much going to get the same behavior as ClojureJVM.  Except:
 

|     | JVM | CLR |
|-----|-----|-----|
| `(short -32768.000001)` | throws an exception |  => -32768   |
| ` (short 32767.000001)`  | throws an exception | => 128 |

However, the behavior is not entirely consistent between the two platforms. In particular, the handling of floating-point numbers during casting can lead to different results. For example:

|     | JVM | CLR |
|-----|-----|-----|
| `(short -32769.0)` | throws an exception | throws an exception   |
| `(short 32768.0)`  | throws an exception | throws an exception |

This comes down to a fundamental difference between

```Java
((Number) x).longValue()
```

versus

```C#
 return (long)(double)o;
```

> I consider this a fundamental platform difference.  No change.

The last case involving conversion:

|     | JVM | CLR |
|-----|-----|-----|
| `(short "0")` | throws an exception |  => 0   |

The root cause here is that on the CLR, `System.String` implements `IConvertible` and implements `IConvertible.ToInt64`. 

> This is a case of being more permissive on the CLR.  And it's related to a fundamental mechanism on the platform (`IConvertible`).  No change.


## Infinite problems

ClojureCLR was failing the test for `(parse-double "Infinity")` and `(parse-double "-Infinity")`.
`parse-double` mostly is a wrapper for `Double.Parse`.  Upon reading the doc for that method, one finds that it is indeed supposed to handle strings representing positive and negative infinity.  However, the values it looks for are culture-dependent, specifically the values of `NumberFormatInfo.PositiveInfinitySymbol` and `NumberFormatInfo.NegativeInfinitySymbol`.  In the `InvariantCulture`, these are "Infinity" and "-Infinity".  However, in my culture (en-US), they are "∞" and "-∞".   This is not the first time I've been bitten by this.   In fact, take a close look through the code above to see it in use.

> Change the `Parse` calls to use `InvariantCulture`.  (DONE!)

## Quot-idian problems

Problems arose with tests on `quot`: `(let [r (quot 10.0M 3)] (and (decimal? r) (= 3.0M r)))` is false on ClojureCLR.
Easy enough to find the proximal cause:

|     | JVM | CLR |
|-----|-----|-----|
| `(quot 10.0M 3)` | => 3.0M |  => 3.333333333M   |

The `quot` function is defined in Clojure core as

```Clojure
(defn quot
  "quot[ient] of dividing numerator by denominator."
  {:added "1.0"
   :static true
   :inline (fn [x y] `(. clojure.lang.Numbers (quotient ~x ~y)))}
  [num div]
    (. clojure.lang.Numbers (quotient num div)))
```

And we are always on thin ice down in `clojure.lang.Numbers`.
This call will end up in a call to `Numbers.BigDecimalOps.quotient` in both the JVM and the CLR.

```Java
	public Number quotient(Number x, Number y){
		MathContext mc = (MathContext) MATH_CONTEXT.deref();
		return mc == null
		       ? c
		       : toBigDecimal(x).divideToIntegralValue(toBigDecimal(y), mc);
	}

```


```C#
            public override object quotient(object x, object y)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MathContextVar.deref();
                return c == null
                    ? ToBigDecimal(x).DivideInteger(ToBigDecimal(y))
                    : ToBigDecimal(x).DivideInteger(ToBigDecimal(y), c.Value);
            }
```

In both instances, `*math-context*` is null, so the difference will be found in the calls

| JVM | CLR |
|-----|-----|
| `toBigDecimal(x).divideToIntegralValue(toBigDecimal(y))` | `ToBigDecimal(x).DivideInteger(ToBigDecimal(y))` |

`java.math.BigDecimal` is defined by the JVM.  We can look at its doc:

> Returns a BigDecimal whose value is the integer part of (this / divisor). Since the integer part of the exact quotient does not depend on the rounding mode, the rounding mode does not affect the values returned by this method. The preferred scale of the result is (this.scale() - divisor.scale()). An ArithmeticException is thrown if the integer part of the exact quotient needs more than mc.precision digits.  (DONE!)

For ClojureCLR, it defines its own `BigDecimal` class.  We can look at the source.  Bizarrely, or perhaps just ironically, I took the implementation of `DivideInteger` from the OpenJDK algorithm!

```C#
 /// <summary>
 /// Return the integer part of this / y.
 /// </summary>
 /// <param name="y"></param>
 /// <returns></returns>
 /// <remarks>I am indebted to the OpenJDK implementation for the algorithm.
 /// <para>However, the spec I'm working from specifies an exponent of zero always!
 /// The OpenJDK implementation does otherwise.  So I've modified it to yield a zero exponent.</para>
 /// </remarks>
 public BigDecimal DivideInteger(BigDecimal y)
 {

     // Calculate preferred exponent
     //int preferredExp = (int)Math.Max(Math.Min((long)this._exp - y._exp,
     //                                            Int32.MaxValue), Int32.MinValue);
     int preferredExp = 0;

     if (Abs().CompareTo(y.Abs()) < 0)
     {
         return new BigDecimal(BigInteger.Zero, preferredExp);
     }

     if (this._coeff.IsZero && !y._coeff.IsZero)
         return Rescale(this, preferredExp, RoundingMode.Unnecessary);

     // Perform a divide with enough digits to round to a correct
     // integer value; then remove any fractional digits

     int maxDigits = (int)Math.Min(this.GetPrecision() +
                                   (long)Math.Ceiling(10.0 * y.GetPrecision() / 3.0) +
                                   Math.Abs((long)this._exp - y._exp) + 2,
                                   Int32.MaxValue);

     BigDecimal quotient = this.Divide(y, new Context((uint)maxDigits, RoundingMode.Down));
     if (y._exp < 0)
     {
         quotient = Rescale(quotient, 0, RoundingMode.Down).StripZerosToMatchExponent(preferredExp);
     }

     if (quotient._exp > preferredExp)
     {
         // pad with zeros if necessary
         quotient = Rescale(quotient, preferredExp, RoundingMode.Unnecessary);
     }

     return quotient;
 }        
 ```

Well, I'm sure the bug is obvious.  I made a transcription error.  (Abetted by the fact that I had to translate all the Java code references to `scale` to CLR references to `exponent`, and they are opposite in sign.)  Enough of a hint?

Here it is:

```C#
  if (y._exp < 0)
```

should be:

```C#
  if (quotient._exp < 0)
```

> BUG: Fix the error.  (DONE!)

## Rationalize this

ClojureCLR is getting errors on calls like `(rationalize 1)`: "System.InvalidCastException: Unable to cast object of type 'System.Int64' to type 'clojure.lang.BigDecimal'."  The function `rationalize` just calls `clojure.lang.Numbers.rationalize`.  On the JVM:

```Java
@WarnBoxedMath(false)
static public Number rationalize(Number x){
	if(x instanceof Float || x instanceof Double)
		return rationalize(BigDecimal.valueOf(x.doubleValue()));
	else if(x instanceof BigDecimal)
		{
		BigDecimal bx = (BigDecimal) x;
		BigInteger bv = bx.unscaledValue();
		int scale = bx.scale();
		if(scale < 0)
			return BigInt.fromBigInteger(bv.multiply(BigInteger.TEN.pow(-scale)));
		else
			return divide(bv, BigInteger.TEN.pow(scale));
		}
	return x;
}
```

On the CLR:

```C#
[WarnBoxedMath(false)]
public static object rationalize(object x)
{
    if (x is float f)                              
        return rationalize(BigDecimal.Create(f));

    if (x is double d)                        
        return rationalize(BigDecimal.Create(d));

    BigDecimal bx = (BigDecimal)x;
    if (bx != null)
    {
        int exp = bx.Exponent;
        if (exp >= 0)
            return BigInt.fromBigInteger(bx.ToBigInteger());
        else
            return divide(bx.MovePointRight(-exp).ToBigInteger(), BigInteger.Ten.Power(-exp));
    }

    return x;
}
```

Another obvious error.

```C#
BigDecimal bx = (BigDecimal)x;
```
should be:

```C#
BigDecimal bx = x as BigDecimal
```

If you look around the code in this area, you'll see plenty of places where I do this properly.  This one just slipped through.

> BUG: Fix the error.  (DONE!)

And while we are being rational:  

|     | JVM | CLR |
|-----|-----|-----|
| `(rationalize (/ 1.0 3.0))` | 3333333333333333/10000000000000000 | 6004799503160661/18014398509481984 |

Surely, you say, the JVM must be correct; after all, that's what I would compute.
However, the second one is actually more accurate.  I converted each to `BigDecimal`:

```Clojure
(bigdec 3333333333333333/10000000000000000) ; => 0.3333333333333333M
(bigdec 6004799503160661/18014398509481984) ; => 0.333333333333333314829616256247390992939472198486328125M
```
On both the JVM and the CLR.
So the weird one is actually more accurate.
Is this a bug?

>  I'm not going to touch this one.

## The precise problem

All the `with-precision` tests will fail becauses we use a different type for specifying the precision.
On the JVM, precision values are encoded as properties of `java.math.RoundingMode`.
On the CLR, we have an `enum` `clojure.lang.BigDecimal+RoundingMode`.  
The underlying details (properties or enum values) are hidden by the `with-precision` macro.
However, I chose to not to hid the difference in names.

| JVM | CLR |
|-----|-----|
| `CEILING` | `Ceiling` |
| `DOWN` | `Down` |
| `FLOOR` | `Floor` |
| `HALF_DOWN` | `HalfDown` |
| `HALF_EVEN` | `HalfEven` |
| `HALF_UP` | `HalfUp` |
| `UNNECESSARY` | `Unnecessary` |
| `UP` | `Up` |

> It would be easy enough to modify `with-precision` to allow either naming.  One less difference for porting.  I'll think about it.

## String tests

ClojureCLR fails a number of tests for functions in `clojure.string`.  
These tests share the common feature that they pass into a `clojure.string` function such as `capitalize` arguments that are not strings.
For example,

```clojure
    #?(:cljs (do (is (thrown? :default (str/capitalize 1)))
                 (is (thrown? :default (str/capitalize 'a)))
                 (is (thrown? :default (str/capitalize 'a/a)))
                 (is (thrown? :default (str/capitalize :a)))
                 (is (thrown? :default (str/capitalize :a/a))))
       :default (do (is (= "1" (str/capitalize 1)))
                    (is (= "Asdf" (str/capitalize 'Asdf)))
                    (is (= "Asdf/asdf" (str/capitalize 'asDf/aSdf)))
                    (is (= ":asdf/asdf" (str/capitalize :asDf/aSdf)))))
```

My question is whether the `:default` tests are proper.
I discussed the purpose of these test with Jeaye Wilkerson.  His intention that the tests should be descriptive of the behavior of each dialect; they are not normative.  

Now I can just copy the `:cljs` tests for `:cljr` and be done.  


## Some true breakage

### aset

My initial run of the tests indicate two places where ClojureCLR has non-trivial bugs.
One is in the test for `aclone`.  I have done a little investigation and can reproduce the issue with a small piece of code.

```clojure
(defn bad-clone [a]
    (aset a 0 1)
    (let [a' (aclone a)]
      (aset a' 0 2)
      a'))
```

Consider calling `(bad-clone (int-array 3))`.
The first call to `aset` goes fine.  It resolves dynamically at run-time (no type information on `a` to avoid reflection = dynamic callsite) to a call to `RT.aset(int[],int,int)`.  That works.

The second call to `aset` blows up with an exception relating to widening types.  Actually it's a problem with narrowing. Because the `aclone` calls inlines to a call to `Array RT.aclone(Array)`, we have an `Array` type hint on `a'`.  So the call to `aset` resolves to `RT.aset(Array,object,int)`.  This method calls `Array.SetValue`.  And that call chokes because the value being inserted into the array is an `Int64` and we have an array of `Int32`!

If instead we define

```clojure
(def ok-clone [a]
    (aset a 0 1)
    (let [a' (aclone a)]
      (aset a' 0 (int 2))
      a'))
```

the call `(ok-clone (int-array 3))` works fine.

I'm not sure what the best solution is here.  Forcing the user to know that the conversion is necessary in one call to `aset` but not the other strikes me as bad.  
<strike>The easiest solution is change `RT.aclone` to have return type `Object` instead of `Array`.  We get a dynamic callsite in the second `aset` call, but that's probably better than the reflection that takes place in `Array.SetValue`.  </strike>

Well, that definitely did not work.  The real solution has several parts. 

- Get rid of `RT.aclone(Array)`
- Add overloads for `RT.aclone` for all the basic array types used for the other operations: `object[]`, `int[]`, `long[]`, `float[]`, `double[]`, etc.
- Get rid of `RT.aset(Array a, int idx, object val)`.

We will get reflection warnings -- can't be helped.  So does ClojureJVM, on the same calls.  So I think we are good to go.

Oh, and 'gvec.clj` compiles without any warnings.  That lib has always been a problem child, particularly for the array functions.  So, score.

Oh, and all the standard Clojure tests run successfully and with no warnings.  Score again.

DONE!

### case

The other problem area is with `case`.   It should be noted that `case` passes all the tests that are in the test suite in the Clojure repo.  There is something about the specific cases in these tests that cause `case` to fail.  This will not be fun.

The major test that fails is just a big case statement with lots of different types of test cases.  I started with an empty case statement and started adding test/then pairs until I got a failure, then started removing items until I got a minimal failure case.  Here it is:

```clojure
(defn f
    [x]
    (case x
        3.0 :double-result      
        true :boolean-true-result
        false :boolean-false-result
        :default))

(map f '(3.0 true false)) ; => (:default :boolean-true-result :boolean-false-result)
```
The argument `3.0` does not match its case.

Further investigation reveals that is related to the `:sparse-key` switch type in `case`.
Looking at the compiled code, it is clearly hosed.

> THe only solution is to hard-code binary search through the hash codes.  DONE!
