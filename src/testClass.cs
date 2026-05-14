namespace src;

public class testClass
{
    public string name { get; set; }
    public int age { get; set; }    

    public static void printInfo()
    {
        // prints info
        Console.WriteLine($"Name: {name}, Age: {age}");
    }
    public static void printAnotherThing()
    {
        // prints another thing
        Console.WriteLine("This is another thing.");
    }
    public static void printYetAnotherThing()
    {
        // prints yet another thing
        Console.WriteLine("This is yet another thing.");
    }

}