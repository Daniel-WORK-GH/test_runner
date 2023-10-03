# test_runner
This project is a test tool that will run automatically methonds with a specific attribute. The test tool file is 'TestRunner.cs'

To create a test method first create a class that inherits from 'TestClass'.
You can give this class a title using the 'TestTitle' attribute.

![image](https://github.com/Daniel-WORK-GH/test_runner/assets/120199463/110e6a98-1d45-4d54-8944-851b2adc3f31)

Then create the test method, it needs to be public static and have the 'TestAtr' attribute.
Use the testing tools: 
 - assert
 - check_not_throws
 - check_throws
 - check_throws_as

![image](https://github.com/Daniel-WORK-GH/test_runner/assets/120199463/c20e1519-6f0c-4a3f-92a1-97aa669fd24f)

To run all existing tests call 'TestRunner.Run();' in the main function and it will automatically locate every test method and call it.
The result will be printed in the console/termial. 

![image](https://github.com/Daniel-WORK-GH/test_runner/assets/120199463/10048a92-7d57-455a-8f57-a37dd80347e6)

To run a specific test class, use 'TestRunner.Run<[class name]>();' for example 'TestRunner.Run< TestExample >();'
