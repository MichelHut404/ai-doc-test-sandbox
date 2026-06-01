namespace src;

public class Zoo
{
        //test

    private readonly string _name;
    private readonly List<IAnimal> _animals = new();

    public Zoo(string name)
    {
        _name = name;
    }

    public void AddAnimal(IAnimal animal)
    {
        _animals.Add(animal);
        Console.WriteLine($"[{_name}] {animal.Name} is toegevoegd aan de dierentuin.");
    }

    public void OpenDeuren()
    {
        Console.WriteLine($"\n=== Welkom bij {_name}! ===");
        Console.WriteLine($"We hebben vandaag {_animals.Count} dieren.\n");

        foreach (var animal in _animals)
        {
            Console.WriteLine($"--- {animal.GetDescription()} ---");
            animal.MakeSound();
            animal.Eat(animal.Species == "Olifant" ? "banaan" : "vlees");
            Console.WriteLine();
        }
    }

    public void SluitDeuren()
    {
        Console.WriteLine($"=== {_name} sluit voor vandaag. Tot morgen! ===");
    }

    public static void Main(string[] args)
    {
        var zoo = new Zoo("Artis Amsterdam");

        zoo.AddAnimal(new Animal("Simba", "Leeuw", 5, "ROAAR", "vlees"));
        zoo.AddAnimal(new Animal("Dumbo", "Olifant", 12, "TOEEET", "banaan"));
        zoo.AddAnimal(new Animal("Pingu", "Pinguïn", 3, "KWAK KWAK", "vis"));
        zoo.AddAnimal(new Animal("Giraf Gijs", "Giraf", 8, "hmmmm", "bladeren"));

        zoo.OpenDeuren();
        zoo.SluitDeuren();
    }
}
