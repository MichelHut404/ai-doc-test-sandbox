namespace MyTestProject;

public class Class2
{
    public void anotherMethod()
    {
        Console.WriteLine("This is another method in a different class.");
        Console.WriteLine("It serves to test documentation generation across multiple classes.");
    }

    public void interfaceMethod()
    {
        Console.WriteLine("This is another implementation of the interface method in a different class.");
    }

    public void methodWithParameters(string param1, int param2)
    {
        Console.WriteLine($"This method takes parameters: {param1} and {param2}");
    }

}