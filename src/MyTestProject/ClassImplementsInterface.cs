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
    public void main()
    {
        DoSomething();
        doAnotherThing();
        DoSomethingElse();
    }
}