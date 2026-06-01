namespace src;

public class Animal : IAnimal
{
    public string Name { get; }
    public string Species { get; }
    public int Age { get; }

    private readonly string _sound;
    private readonly string _favoriteFood;

    public Animal(string name, string species, int age, string sound, string favoriteFood)
    {
        Name = name;
        Species = species;
        Age = age;
        _sound = sound;
        _favoriteFood = favoriteFood;
    }

    public void MakeSound()
    {
        Console.WriteLine($"{Name} zegt: {_sound}!");
    }

    public void Eat(string food)
    {
        if (food == _favoriteFood)
            Console.WriteLine($"{Name} eet gretig {food}. Lekker!");
        else
            Console.WriteLine($"{Name} snuffelt aan {food}... maar eet het niet op.");
    }

    public string GetDescription()
    {
        return $"{Name} ({Species}), {Age} jaar oud";
    }
}
