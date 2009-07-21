/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */

package testjavaformat;

import java.io.IOException;
import java.util.Calendar;
import java.util.Date;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 *
 * @author David Miller
 */
public class Main {

    /**
     * @param args the command line arguments
     */
    public static void main(String[] args) {
        RunFormatTests();
    }

    private static void RunFormatTests() {
        Test("");
        Test("abc");
        Test("abc%%def");
        Test("abc%ndef");
        Test("%%%%");
        Test("%d",12);
        Test("abc%3$ddef%1$dghi%2$d", 12, 13, 14);
        Test("abc%ddef%<dghi%d", 12, 13, 14);
        Test("abc%0$ddef", 12);
        Test("%b",null);

                 Test("%o", (byte)-12);
            Test("%o", (short)-12);
            Test("%o", (int)-12);
            Test("%o", (long)-12);

                             Test("%x", (byte)-12);
            Test("%x", (short)-12);
            Test("%x", (int)-12);
            Test("%x", (long)-12);

            Test( "%012d", 12345678);
            Test("%012d", -12345678);

            Test("%o",12345);
            Test("%o", -12345);
            Test("%12o", 12345);

            Test( "%o", 12345);
            Test( "%o", -12345);
            Test( "%12o", 12345);

            Test("%015x", 12347);
            Test( "%015x", -12347);

            Test( "%0#15x", 12347);
            Test("%0#15x", -12347);

            Test("%,12.4f", 1234.56789);
            Test("%#12.2f", 1234.56789);
            Test("%+12.2f", 1234.56789);
            Test("% 12.2f", 1234.56789);
            Test("%012.2f", 1234.56789);
            Test("%(12.2f", -1234.56789);

            Test("%12.4e",1.23456789e0);
            Test("%12.4e",1.23456789e1);
            Test("%12.4e",1.23456789e2);
            Test("%12.4e",1.23456789e3);
            Test("%12.4e",1.23456789e4);
            Test("%12.4e",1.23456789e5);
            Test("%12.4e",1.23456789e6);
            Test("%12.4e",1.23456789e7);
            Test("%12.4e",1.23456789e8);
            Test("%12.4e",1.23456789e9);
            Test("%12.4e",1.23456789e10);
            Test("%12.4e",1.23456789e-1);
            Test("%12.4e",1.23456789e-2);
            Test("%12.4e",1.23456789e-3);
            Test("%12.4e",1.23456789e04);
            Test("%12.4e",1.23456789e-5);
            Test("%12.4e",1.23456789e-6);
            Test("%12.4e",1.23456789e-7);
            Test("%12.4e",1.23456789e-8);
            Test("%12.4e",1.23456789e-9);
            Test("%12.4e",1.23456789e-10);

            //Test("%,12.4e", 1234.56789);
            Test("%#12.2e", 1234.56789);
            Test("%+12.2e", 1234.56789);
            Test("% 12.2e", 1234.56789);
            Test("%012.2e", 1234.56789);
            Test("%(12.2e", -1234.56789);

            Test("%12.4a",1.23456789e0);
            Test("%12.4a",1.23456789e1);
            Test("%12.4a",1.23456789e2);
            Test("%12.4a",1.23456789e3);
            Test("%12.4a",1.23456789e4);
            Test("%12.4a",1.23456789e5);
            Test("%12.4a",1.23456789e6);
            Test("%12.4a",1.23456789e7);
            Test("%12.4a",1.23456789e8);
            Test("%12.4a",1.23456789e9);
            Test("%12.4a",1.23456789e10);
            Test("%12.4a",1.23456789e-1);
            Test("%12.4a",1.23456789e-2);
            Test("%12.4a",1.23456789e-3);
            Test("%12.4a",1.23456789e04);
            Test("%12.4a",1.23456789e-5);
            Test("%12.4a",1.23456789e-6);
            Test("%12.4a",1.23456789e-7);
            Test("%12.4a",1.23456789e-8);
            Test("%12.4a",1.23456789e-9);
            Test("%12.4a",1.23456789e-10);

            Test("%12.4g",1.23456789e0);
            Test("%12.4g",1.23456789e1);
            Test("%12.4g",1.23456789e2);
            Test("%12.4g",1.23456789e3);
            Test("%12.4g",1.23456789e4);
            Test("%12.4g",1.23456789e5);
            Test("%12.4g",1.23456789e6);
            Test("%12.4g",1.23456789e7);
            Test("%12.4g",1.23456789e8);
            Test("%12.4g",1.23456789e9);
            Test("%12.4g",1.23456789e10);
            Test("%12.4g",1.23456789e-1);
            Test("%12.4g",1.23456789e-2);
            Test("%12.4g",1.23456789e-3);
            Test("%12.4g",1.23456789e-4);
            Test("%12.4g",1.23456789e-5);
            Test("%12.4g",1.23456789e-6);
            Test("%12.4g",1.23456789e-7);
            Test("%12.4g",1.23456789e-8);
            Test("%12.4g",1.23456789e-9);
            Test("%12.4g",1.23456789e-10);


            Test("%,12.4g", 1.23456789e0);
            Test("%+12.4g", 1.23456789e0);
            Test("% 12.4g", 1.23456789e0);
            Test("%012.4g", 1.23456789e0);
            Test("%(12.4g", -1.23456789e0);

            Test("%,12.4g", 1.23456789e3);
            Test("%+12.4g", 1.23456789e3);
            Test("% 12.4g", 1.23456789e3);
            Test("%012.4g", 1.23456789e3);
            Test("%(12.4g", -1.23456789e3);

            Test("%,12.4g", 1.23456789e-3);
            Test("%+12.4g", 1.23456789e-3);
            Test("% 12.4g", 1.23456789e-3);
            Test("%012.4g", 1.23456789e-3);
            Test("%(12.4g", -1.23456789e-3);


            Test("%,12.4g", 1.23456789e7);
            Test("%+12.4g", 1.23456789e7);
            Test("% 12.4g", 1.23456789e7);
            Test("%012.4g", 1.23456789e7);
            Test("%(12.4g", -1.23456789e7);

            Test("%,12.4g", 1.23456789e-7);
            Test("%+12.4g", 1.23456789e-7);
            Test("% 12.4g", 1.23456789e-7);
            Test("%012.4g", 1.23456789e-7);
            Test("%(12.4g", -1.23456789e-7);

            Test("%c",0x1BCD);
            String s = String.format("%c",0x1BCD);
            System.out.println("char0 =" + String.format("%x", (int) s.charAt(0)));
            //System.out.println("char1 =" + String.format("%x", (int) s.charAt(1)));


            Test("%,20d",new java.math.BigInteger("123456789"));

            System.out.println("-------------");

            Test("%tH", new Date(2009, 7, 1, 1, 10, 20));
            Test("%tH", new Date(2009, 7, 1, 14, 10, 20));   
            Test("%tI", new Date(2009, 7, 1, 1, 10, 20));
            Test("%tI", new Date(2009, 7, 1, 14, 10, 20));
            Test("%tk", new Date(2009, 7, 1, 1, 10, 20));
            Test("%tk", new Date(2009, 7, 1, 14, 10, 20));
            Test("%tl", new Date(2009, 7, 1, 1, 10, 20));
            Test("%tl", new Date(2009, 7, 1, 14, 10, 20));

            Test("%tM", new Date(2009, 7, 1, 14, 02, 03));
            Test("%tM", new Date(2009, 7, 1, 14, 44, 50));
            Test("%tS", new Date(2009, 7, 1, 14, 02, 03));
            Test("%tS", new Date(2009, 7, 1, 14, 44, 50));
            





    }

    private static void Test(String fmt, Object ... args) {
        try {
            System.out.print("Trying /" + fmt + "/(");
            for ( Object arg : args)
                System.out.print(arg.toString() + ",");
            System.out.print("): ");
            System.out.print("/" + String.format(fmt,args) + "/");
            System.out.println();
        }
        catch (Exception e)
        {
            System.out.println("Threw error: " + e.getClass().toString() + ": " + e.getMessage());
        }
    }


}
