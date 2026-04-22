namespace src.MyTestProject;

public class ClassImplementsInterface : IService
{

    public void DoSomething()
    {
        Console.WriteLine("Doing something...");
    } 

    public void thisNeedsToBeDocumented()
    {
        Console.WriteLine("This method needs to be documented...");
    }

    public void main()
    {
        DoSomething();
    }
}