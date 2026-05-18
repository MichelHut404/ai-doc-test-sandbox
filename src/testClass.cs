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
    public static void printMyName()
    {
        // prints name
        Console.WriteLine($"My name is {name}");
    }

}