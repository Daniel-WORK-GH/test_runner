using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;

namespace Tests
{
    public class TestTitle : Attribute
    {
        public readonly string Title;

        public TestTitle(string title)
        {
            this.Title = title;
        }
    }

    public sealed class TestException
    {
        public readonly string Message;
        public readonly int LineNumber;
        public readonly string CallerMathod;
        public readonly string CallerClass;

        public TestException(string msg, int lineNumber, string callerMethod, string callerClass)
        {
            this.Message = msg;
            this.LineNumber = lineNumber;
            this.CallerMathod = callerMethod;
            this.CallerClass = callerClass;
        }

        public void Log(Queue<TestException> queue)
        {
            queue.Enqueue(this);
        }
    }

    public class TestClass
    {
        protected sealed class TestAtr : Attribute
        {
        }

        protected static void assert(bool value, string msg = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!value)
            {
                TestClass.LogException("Asserting error", msg, lineNumber);
            }
        }

        protected static void check_throws(Action action, string msg = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                action();
                TestClass.LogException("Exception wasn't thrown", msg, lineNumber);
            }
            catch
            {
                // As expected
            }
        }

        protected static void check_not_throws(Action action, string msg = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                action();
            }
            catch(Exception e)
            {
                msg = e.Message + '\n' + msg;
                TestClass.LogException("Exception was thrown", msg, lineNumber);
            }
        }

        protected static void check_throws<T>(Action action, string msg = "",
            [CallerLineNumber] int lineNumber = 0) where T : Exception
        {
            try
            {
                action();
                TestClass.LogException("Exception wasn't thrown", msg, lineNumber);
            }
            catch (T)
            {
                // As expected
            }
            catch (Exception e)
            {
                TestClass.LogException($"{e.GetType()} was thrown instead of {typeof(T)}", msg, lineNumber);
            }
        }

        private static void LogException(string errorType, string message, int lineNumber)
        {
            TestClass.GetCaller(out string callerMethod, out string callerClass);
            message = TestClass.GetErrorString(errorType, message.Trim(), lineNumber, callerMethod, callerClass);
            TestException ex = new TestException(message, lineNumber, callerMethod, callerClass);
            ex.Log(TestRunner.FailedTests);
        }

        private static void GetCaller(out string callerMethod, out string callerClass)
        {
            var methodInfo = new StackTrace()?.GetFrame(3)?.GetMethod();
            callerMethod = methodInfo?.Name ?? "";
            callerClass = methodInfo?.DeclaringType?.Name ?? "";
        }

        private static string GetErrorString(string errorType, string message, int lineNumber, string methodName, string className)
        {
            StringBuilder builder = new StringBuilder($"{errorType} from caller {className}.{methodName} on line {lineNumber}!!");

            if (message != string.Empty)
            {
                builder.Append("\n\t\tError message: ");
                builder.Append(message.Replace("\n", "\n\t\t" + "               "));
            }

            return builder.ToString();
        }
    }

    public sealed class TestRunner : TestClass
    {
        public static bool IsRunning { get; private set; }

        public static readonly Queue<TestException> FailedTests;

        static TestRunner()
        {
            TestRunner.IsRunning = false;
            TestRunner.FailedTests = new Queue<TestException>();
        }

        public static void Run()
        {
            var methods = TestRunner.StartTestRun();

            TestRunner.RunBasicTestLoop(methods);
        }

        public static void Run<T>() where T : TestClass
        {
            var methods = TestRunner.StartTestRun<T>();

            TestRunner.RunBasicTestLoop(methods);
        }


        private static void RunBasicTestLoop(IEnumerable<MethodInfo?> methods)
        {
            bool totalStatus = true;
            Type classType = typeof(TestRunner);

            foreach (var method in methods) // iterate through all found methods
            {
                if (method == null || method.DeclaringType == null) continue;

                // Handle printing titles
                TestRunner.TryPrintTitle(method, ref classType);

                TestRunner.HandleTestMethod(method, out bool status);

                totalStatus = totalStatus && status;
            }

            TestRunner.EndTestRun(totalStatus);
        }

        private static IEnumerable<MethodInfo?> StartTestRun()
        {
            if (IsRunning) throw new Exception("Tests already running.");

            TestRunner.IsRunning = true;

            var methods = AppDomain.CurrentDomain.GetAssemblies() // Returns all currenlty loaded assemblies
                .SelectMany(x => x.GetTypes()) // returns all types defined in this assemblies
                .Where(x => x.IsClass) // only yields classes
                .SelectMany(x => x.GetMethods()) // returns all methods defined in those classes
                .Where(x => x.GetCustomAttributes(typeof(TestAtr), false).FirstOrDefault() != null); // returns only methods that have the TestAtr
        
            return methods;
        }

        private static IEnumerable<MethodInfo?> StartTestRun<T>()
        {
            if (IsRunning) throw new Exception("Tests already running.");

            TestRunner.IsRunning = true;

            var methods = AppDomain.CurrentDomain.GetAssemblies() // Returns all currenlty loaded assemblies
                .SelectMany(x => x.GetTypes()) // returns all types defined in this assemblies
                .Where(x => x.IsClass) // only yields classes
                .SelectMany(x => x.GetMethods()) // returns all methods defined in those classes
                .Where(x => x.DeclaringType == typeof(T))
                .Where(x => x.GetCustomAttributes(typeof(TestAtr), false).FirstOrDefault() != null); // returns only methods that have the TestAtr
        
            return methods;
        }

        private static void TryPrintTitle(MethodInfo method, ref Type previousDeclaringType)
        {
            if (method.DeclaringType != previousDeclaringType && method.DeclaringType != null)
            {
                previousDeclaringType = method.DeclaringType;

                Attribute? atr = Attribute.GetCustomAttribute(method.DeclaringType, typeof(TestTitle));

                if (atr != null)
                {
                    TestTitle tatr = (TestTitle)atr;

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    System.Console.WriteLine($"\n____{tatr.Title}____");
                }
            }
        }

        private static void HandleTestMethod(MethodInfo method, out bool status)
        {
            status = true;
            Console.ForegroundColor = ConsoleColor.White;
            System.Console.Write($"Running test: {method.Name}..");
            try
            {
                if(method.DeclaringType == null)
                {
                    throw new Exception("Unknown test method declaring type");
                }

                var obj = Activator.CreateInstance(method.DeclaringType); // Instantiate the class
                                                                          // var response = method.Invoke(obj, null); // invoke the method
                
                Action action;
                if(method.IsStatic) action = (Action)Delegate.CreateDelegate(typeof(Action), method);
                else action = (Action) Delegate.CreateDelegate(typeof(Action), Activator.CreateInstance(method.DeclaringType), method);

                int currentExceptions = TestRunner.FailedTests.Count;
                action();
                int newExceptions = TestRunner.FailedTests.Count;

                if (currentExceptions == newExceptions)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine("Success!");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine("Failed!");
                }
            }
            catch (Exception e)
            {
                status = false;
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine($"\n\tError occurred: {e.Message}");
            }

            if (TestRunner.FailedTests.Count != 0)
            {
                status = false;
            }
            while (TestRunner.FailedTests.Count != 0)
            {
                TestException e = TestRunner.FailedTests.Dequeue();
                Console.ForegroundColor = ConsoleColor.Yellow;
                System.Console.WriteLine("\n\t" + e.Message);

                if (TestRunner.FailedTests.Count == 0) System.Console.WriteLine();
            }
        }
    
        private static void EndTestRun(bool finalStatus)
        {
            if (finalStatus)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine("\nTests passed.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("\nTests failed.");
            }

            TestRunner.IsRunning = false;
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}