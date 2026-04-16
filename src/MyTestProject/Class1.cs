namespace MyTestProject;

public class Class1 : InterfaceTest
{
    public void someMethod()
    {
        Console.WriteLine("Hello World");
        Console.WriteLine("This is a test method for documentation generation.");
    }

    public void interfaceMethod()
    {
        Console.WriteLine("This is an implementation of the interface method.");
    }

    public void anotherTestingMethod()
    {
        Console.WriteLine("This is another method in the same class.");
        Console.WriteLine("It serves to test documentation generation across multiple methods.");
    }
}
