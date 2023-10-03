namespace Tests
{
    [TestTitle("Showing example test structure")]
    public class TestExample : TestClass {

        [TestAtr]
        public static void Test_Number_Addition()
        {
            assert(1 + 1 == 2);

            assert(1 + 3 != 3);
        }

        [TestAtr]
        public static void Test_Number_Division()
        {
            int zero = 0;
            
            check_not_throws(() => { int res = 1 + 1; });

            check_throws(() => { int res = 1 / zero; }); 

            check_throws<DivideByZeroException>(() => { int res = 1 / zero; });
        }
    }
}