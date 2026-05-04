namespace src;

public class testClass
{
    public string name { get; set; }
    public int age { get; set; }    

    public void printInfo()
    {
        // prints info
        Console.WriteLine($"Name: {name}, Age: {age}");
    }

}