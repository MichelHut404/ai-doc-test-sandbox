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
    public void testMethod()
    {
        // does something
        Console.WriteLine("This is a test method.");
    }

}