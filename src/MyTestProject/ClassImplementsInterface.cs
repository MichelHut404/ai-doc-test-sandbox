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
}