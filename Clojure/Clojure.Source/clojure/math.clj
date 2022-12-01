;   Copyright (c) Rich Hickey. All rights reserved.
;   The use and distribution terms for this software are covered by the
;   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
;   which can be found in the file epl-v10.html at the root of this distribution.
;   By using this software in any fashion, you are agreeing to be bound by
;   the terms of this license.
;   You must not remove this notice, or any other, from this software.

(ns
  ^{:author "David Miller",
    :doc "Clojure wrapper functions for System.Math static methods.

  Function calls are inlined for performance, and type hinted for primitive
  long or double parameters where appropriate. In general, Math methods are
  optimized for performance and have bounds for error tolerance. 

  For more complete information, see:
  https://docs.microsoft.com/en-us/dotnet/api/system.math?view=net-6.0"}
  clojure.math)

  (set! *warn-on-reflection* true)


  ;;; I followed the overall style of clojure.java.math, but the java.lang.math and System.Math do not have the same methods defined.
  ;;; Given that this is about giving performant access, I stuck with matching what is in System.Math.
  ;;; Generally, System.Math has a lot more overloads; nevertheless, java.lang.math does have some overloads.
  ;;; clojure.java.math deals with overloads as follows:
  ;;;   (i)   if a method is overloaded for double vs float args only, then a double version is typed.
  ;;;   (ii)  if a method is overloaded for long vs int args only, then a long version is typed.
  ;;;   (iii) the serveral methods that have multiple overloads, such as min, max, and max, are not included here.  they are handled in Numbers.
  ;;;
  ;;; Note that (i) and (ii) are quite reasonble given generally that int and float values get widened to long/double.
  ;;;
  ;;; The System.Math package generally has a lot more overloads.
  ;;; We follow all three rules listed above. 
  ;;; There are still some cases that are not dealt with by those rules.
  ;;;
  ;;; The following have overloads for double and decimal:
  ;;;    Ceiling
  ;;;    Floor
  ;;;    Round
  ;;;    Truncate
  ;;; I have provided implementations for double arguments only.
  ;;;   
  ;;; The following have many overloads:
  ;;;    Clamp  (13 overloads)
  ;;;    Sign  (8 overloads)
  ;;;
  ;;;  I have decided to pick just a few, using the suffixes Long and Double, i.e.
  ;;;    clamp-long, clamp-double, sign-long, sign-double.
  ;;;
  ;;; I have left out DivRem and BigMul because of the out parameters.  Inlining problematic for those.


  (def
  ^{:doc "Constant for e, the base for natural logarithms.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.e?view=net-6.0"
    :added "1.11"
    :const true
    :tag 'double}
  E
  Math/E)

(def
  ^{:doc "Constant for pi, the ratio of the circumference of a circle to its diameter.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.pi?view=net-6.0"
    :added "1.11"
    :const true
    :tag 'double}
  PI
  Math/PI)


(compile-when (>= (.CompareTo ^String dotnet-version "5") 0)

(def
  ^{:doc "Constant for tau, the number of radians in one turn.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.tau?view=net-6.0"
    :added "1.11"
    :const true
    :tag 'double}
  Tau
  Math/Tau)

) ;; compile-when


;;; Trig

(defn sin
  {:doc "Returns the sine of the specified angle.
  If a is equal to NaN, NegativeInfinity, or PositiveInfinity, this method returns NaN.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.sin?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [a] `(Math/Sin (double ~a)))
   :added "1.11"}
  ^double [^double a]
  (Math/Sin a))

(defn cos
  {:doc "Returns the cosine of the specified angle.
  If a is equal to NaN, NegativeInfinity, or PositiveInfinity, this method returns NaN.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.cos?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [a] `(Math/Cos (double ~a)))
   :added "1.11"}
  ^double [^double a]
  (Math/Cos a))

(defn tan
  {:doc "Returns the tangent of the specified angle.
  If a is equal to NaN, NegativeInfinity, or PositiveInfinity, this method returns NaN.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.tan?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [a] `(Math/Tan (double ~a)))
   :added "1.11"}
  ^double [^double a]
  (Math/Tan a))

(defn asin
  {:doc "Returns the arc sine of an angle, in the range -pi/2 to pi/2.
  NaN if a < -1 or a > 1 or a equals NaN.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.asin?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [a] `(Math/Asin (double ~a)))
   :added "1.11"}
  ^double [^double a]
  (Math/Asin a))

(defn acos
  {:doc "Returns the arc cosine of a, in the range 0.0 to pi.
  NaN if a < -1 or a > 1 or a equals NaN.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.acos?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [a] `(Math/Acos (double ~a)))
   :added "1.11"}
  ^double [^double a]
  (Math/Acos a))

(defn atan
  {:doc "Returns the arc tangent of a, in the range of -pi/2 to pi/2.
  NaN if a equals NaN, 
  -π/2 rounded to double precision (-1.5707963267949) if a equals NegativeInfinity, or 
  π/2 rounded to double precision (1.5707963267949) if a equals PositiveInfinity.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.atan?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [a] `(Math/Atan (double ~a)))
   :added "1.11"}
  ^double [^double a]
  (Math/Atan a))

(defn atan2
  {:doc "Returns the angle theta from the conversion of rectangular coordinates (x, y) to polar coordinates (r, theta).
  Computes the phase theta by computing an arc tangent of y/x in the range of -pi to pi.
  For more details on special cases, see: https://docs.microsoft.com/en-us/dotnet/api/system.math.atan2?view=net-6.0"
   :inline-arities #{2}
   :inline (fn [y x] `(Math/Atan2 (double ~y) (double ~x)))
   :added "1.11"}
  ^double [^double y ^double x]
  (Math/Atan2 y x))


  ;;; hyperbolic functions

(defn sinh
  {:doc "Returns the hyperbolic sine of x, (e^x - e^-x)/2.
  If x is ##NaN => ##NaN
  If x is ##Inf or ##-Inf or zero => x
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.sinh?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [x] `(Math/Sinh (double ~x)))
   :added "1.11"}
  ^double [^double x]
  (Math/Sinh x))

(defn cosh
  {:doc "Returns the hyperbolic cosine of x, (e^x + e^-x)/2.
  NaN if x < 1 or x equals NaN.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.cosh?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [x] `(Math/Cosh (double ~x)))
   :added "1.11"}
  ^double [^double x]
  (Math/Cosh x))

(defn tanh
  {:doc "Returns the hyperbolic tangent of x, sinh(x)/cosh(x).
  If x is ##NaN => ##NaN
  If x is ##Inf => +1.0
  If x is ##-Inf => -1.0
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.tanh?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [x] `(Math/Tanh (double ~x)))
   :added "1.11"}
  ^double [^double x]
  (Math/Tanh x))

(compile-when (contains? #{:dotnet :core} dotnet-platform)

(defn asinh
  {:doc "Returns the angle whose hyperbolic sine is the specified number.
  If x is ##NaN => ##NaN
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.asinh?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [x] `(Math/Asinh (double ~x)))
   :added "1.11"}
  ^double [^double x]
  (Math/Asinh x))

(defn acosh
  {:doc "Returns the angle whose hyperbolic cosine is the specified number.
  If x is ##NaN => ##NaN
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.acosh?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [x] `(Math/Acosh (double ~x)))
   :added "1.11"}
  ^double [^double x]
  (Math/Acosh x))

(defn atanh
  {:doc "Returns the angle whose hyperbolic tangent is the specified number.
  NaN if d < -1 or d > 1 or d equals NaN.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.atanh?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [x] `(Math/Atanh (double ~x)))
   :added "1.11"}
  ^double [^double x]
  (Math/Atanh x))

) ;; compile-when

;;; Exponentials and logarithms

(defn exp
  {:doc "Returns Euler's number e raised to the power of a.
  If a is ##NaN => ##NaN
  If a is ##Inf => ##Inf
  If a is ##-Inf => +0.0
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.exp?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [a] `(Math/Exp (double ~a)))
   :added "1.11"}
  ^double [^double a]
  (Math/Exp a))

(defn log
  {:doc "Returns the natural logarithm (base e) of a, or in the specified base.
  If a is ##NaN or negative => ##NaN
  If a is ##Inf => ##Inf
  If a is zero => ##-Inf
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.log?view=net-6.0"
   :inline-arities #{1 2}
   :inline (fn [a & args] `(Math/Log (double ~a) ~@args))
   :added "1.11"}
  (^double [^double a] (Math/Log a))
  (^double [^double a b] (Math/Log a b)))

(defn log10
  {:doc "Returns the logarithm (base 10) of a.
  If a is ##NaN or negative => ##NaN
  If a is ##Inf => ##Inf
  If a is zero => ##-Inf
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.log10?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [a] `(Math/Log10 (double ~a)))
   :added "1.11"}
  ^double [^double a]
  (Math/Log10 a))

(compile-when (contains? #{:dotnet :core} dotnet-platform)

(defn log2
  {:doc "Returns the logarithm (base 2) of a.
  If a is ##NaN or negative => ##NaN
  If a is ##Inf => ##Inf
  If a is zero => ##-Inf
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.log2?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [a] `(Math/Log2 (double ~a)))
   :added "1.11"}
  ^double [^double a]
  (Math/Log2 a))

(defn ilogb
  {:doc "Returns the base 2 integer logarithm of a specified number.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.ilogb?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [a] `(Math/ILogB (double ~a)))
   :added "1.11"}
  [^double a]
  (Math/ILogB a))

  ) ;; compile-when

(defn sqrt
  {:doc "Returns the positive square root of a.
  If a is ##NaN or negative => ##NaN
  If a is ##Inf => ##Inf
  If a is zero => a
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.sqrt?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [a] `(Math/Sqrt (double ~a)))
   :added "1.11"}
  ^double [^double a]
  (Math/Sqrt a))

(compile-when (contains? #{:dotnet :core} dotnet-platform)

(defn cbrt
  {:doc "Returns the cube root of a.
  If a is ##NaN => ##NaN
  If a is ##Inf or ##-Inf => a
  If a is zero => zero with sign matching a
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.cbrt?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [a] `(Math/Cbrt (double ~a)))
   :added "1.11"}
  ^double [^double a]
  (Math/Cbrt a))

) ;; compile-when

  (defn pow
  {:doc "Returns the value of a raised to the power of b.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.pow?view=net-6.0"
   :inline-arities #{2}
   :inline (fn [a b] `(Math/Pow (double ~a) (double ~b)))
   :added "1.11"}
  ^double [^double a ^double b]
  (Math/Pow a b))

  ;;; Division, remainder, rounding, etc.

  (defn IEEE-remainder
  {:doc "A number equal to x - (y Q), where Q is the quotient of x / y rounded to the nearest integer.
  If x / y falls halfway between two integers, the even integer is returned.
  If x - (y Q) is zero, the value +0 is returned if x is positive, or -0 if x is negative.
  If y = 0, NaN is returned.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.ieeeremainder?view=net-6.0"
   :inline-arities #{2}
   :inline (fn [dividend divisor] `(Math/IEEERemainder (double ~dividend) (double ~divisor)))
   :added "1.11"}
  ^double [^double dividend ^double divisor]
  (Math/IEEERemainder dividend divisor))

  (defn ceiling
  {:doc "Returns the smallest integral value greater than or equal to the specified number.
  mathematical integer.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.ceiling?view=net-6.0
  We pick the overload for the double argument."
   :inline-arities #{1}
   :inline (fn [a] `(Math/Ceiling (double ~a)))
   :added "1.11"}
  ^double [^double a]
  (Math/Ceiling a))

(defn floor
  {:doc "Returns the largest integral value less than or equal to the specified number.
  mathematical integer.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.floor?view=net-6.0
  We pick the overload for the double argument."
   :inline-arities #{1}
   :inline (fn [a] `(Math/Floor (double ~a)))
   :added "1.11"}
  ^double [^double a]
  (Math/Floor a))

(defn round
  {:doc "Rounds a value to the nearest integer or to the specified number of fractional digits.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.round?view=net-6.0
  We pick the overloads where the first argument is a double."
   :inline-arities #{1 2 3}
   :inline (fn [a & args] `(Math/Round (double ~a) ~@args))
   :added "1.11"}
  (^double [^double a] (Math/Round a))
  (^double [^double a b] (Math/Round a (int b)))
  (^double [^double a b c] (Math/Round a (int b) ^System.MidpointRounding c)))

  (defn truncate
  {:doc "Integer division that rounds to negative infinity (as opposed to zero).
  The special case (floorDiv Long/MIN_VALUE -1) overflows and returns Long/MIN_VALUE.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.truncate?view=net-6.0
  We pick the overload for the double argument."
   :inline-arities #{1}
   :inline (fn [x] `(Math/Truncate (double ~x)))
   :added "1.11"}
  ^double [^double x]
  (Math/Truncate x))

  (compile-when (contains? #{:dotnet :core} dotnet-platform)

  (defn scaleb
  {:doc "Returns d * 2^scaleFactor, scaling by a factor of 2.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.scaleb?view=net-6.0"
   :inline-arities #{2}
   :inline (fn [d scaleFactor] `(Math/ScaleB (double ~d) (int ~scaleFactor)))
   :added "1.11"}
  ^double [^double d scaleFactor]
  (Math/ScaleB d scaleFactor))

  ) ;; compile-when


  ;;; abs, max,min and related

  ;;; abs, min, max implemented math-polymorphically in clojure.lang.Numbers
  
(defn sign-long
  {:doc "Returns an integer that indicates the sign of a number.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.sign?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [d] `(Math/Sign (long ~d)))
   :added "1.11"}
  [^long d]
  (Math/Sign d))

(defn sign-double
  {:doc "Returns an integer that indicates the sign of a number.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.sign?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [d] `(Math/Sign (double ~d)))
   :added "1.11"}
  [^double d]
  (Math/Sign d))



(compile-when (contains? #{:dotnet :core} dotnet-platform)

(defn max-magnitude
  {:doc "Returns the larger magnitude of two double-precision floating-point numbers.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.maxmagnitude?view=net-6.0"
   :inline-arities #{2}
   :inline (fn [a b] `(Math/MaxMagnitude (double ~a) (double ~b)))
   :added "1.11"}
  ^double [^double a ^double b]
  (Math/MaxMagnitude a b))

(defn min-magnitude
  {:doc "Returns the smaller magnitude of two double-precision floating-point numbers.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.minmagnitude?view=net-6.0"
   :inline-arities #{2}
   :inline (fn [a b] `(Math/MinMagnitude (double ~a) (double ~b)))
   :added "1.11"}
  ^double [^double a ^double b]
  (Math/MinMagnitude a b))

(defn clamp-long
  {:doc "Returns value clamped to the inclusive range of min and max.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.clamp?view=net-6.0"
   :inline-arities #{3}
   :inline (fn [a b c] `(Math/Clamp (long ~a) (long ~b) (long ~c)))
   :added "1.11"}
  ^long [^long val ^long min ^long max]
  (Math/Clamp val min max))

(defn clamp-double
  {:doc "Returns value clamped to the inclusive range of min and max.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.clamp?view=net-6.0"
   :inline-arities #{3}
   :inline (fn [a b c] `(Math/Clamp (double ~a) (double ~b) (double ~c)))
   :added "1.11"}
  ^double [^double val ^double min ^double max]
  (Math/Clamp val min max))

(defn copy-sign
  {:doc "Returns a double with the magnitude of the first argument and the sign of
  the second.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.copysign?view=net-6.0"
   :inline-arities #{2}
   :inline (fn [magnitude sign] `(Math/CopySign (double ~magnitude) (double ~sign)))
   :added "1.11"}
  ^double [^double magnitude ^double sign]
  (Math/CopySign magnitude sign))

(defn bit-decrement
  {:doc "Returns the next smallest value that compares less than d.
  If d is ##NaN => ##NaN
  If d is ##-Inf => ##-Inf
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.bitdecrement?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [d] `(Math/BitDecrement (double ~d)))
   :added "1.11"}
  ^double [^double d]
  (Math/BitDecrement d))

(defn bit-increment
  {:doc "Returns the next largest value that compares greater than d.
  If d is ##NaN => ##NaN
  If d is ##Inf => ##Inf
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.bitincrement?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [d] `(Math/BitIncrement (double ~d)))
   :added "1.11"}
  ^double [^double d]
  (Math/BitIncrement d))

(defn fused-multiply-add
  {:doc "Returns (x * y) + z, rounded as one ternary operation.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.fusedmultiplyadd?view=net-6.0"
   :inline-arities #{3}
   :inline (fn [x y z] `(Math/FusedMultiplyAdd (double ~x) (double ~y) (double ~z)))
   :added "1.11"}
  ^double [^double x ^double y ^double z]
  (Math/FusedMultiplyAdd x y z))
  
) ;; compile-when

(compile-when (>= (.CompareTo ^String dotnet-version "6") 0)

(defn reciprocal-estimate
  {:doc "Returns an estimate of the reciprocal of a specified number.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.reciprocalestimate?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [a] `(Math/ReciprocalEstimate (double ~a)))
   :added "1.11"}
  ^double [^double a]
  (Math/ReciprocalEstimate a))

(defn reciprocal-sqrt-estimate
  {:doc "Returns an estimate of the reciprocal square root of a specified number.
  See: https://docs.microsoft.com/en-us/dotnet/api/system.math.reciprocalsqrtestimate?view=net-6.0"
   :inline-arities #{1}
   :inline (fn [a] `(Math/ReciprocalSqrtEstimate (double ~a)))
   :added "1.11"}
  ^double [^double a]
  (Math/ReciprocalSqrtEstimate a))

) ;; compile-when