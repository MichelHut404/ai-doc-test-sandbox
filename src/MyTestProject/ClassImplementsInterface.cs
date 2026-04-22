namespace src.MyTestProject;

public class ClassImplementsInterface : IService
{

    public void DoSomething()
    {
        Console.WriteLine("Doing something...");
    } 

    public void doAnotherThing()
    {
        Console.WriteLine("Doing another thing...");
    }

    public void DoSomethingElse()
    {
        Console.WriteLine("Doing something else...");
    }
    public void DoSomethingElse2()
    {
        Console.WriteLine("Doing something else 2...");
    }  

    public void thisIsBeingIgnored()
    {
        Console.WriteLine("This method is being ignored...");
    }

    public void DoSomethingElse3()
    {
        Console.WriteLine("Doing something else 3...");
    }

    public void testMethod1()
    {
        Console.WriteLine("This is a test method...");
    }

    public void main()
    {
        DoSomething();
        doAnotherThing();
        DoSomethingElse();
        DoSomethingElse2();
    }
}