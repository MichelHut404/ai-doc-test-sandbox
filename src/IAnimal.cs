namespace src;

public interface IAnimal
{
        //test

    string Name { get; }
    string Species { get; }
    int Age { get; }

    void MakeSound();
    void Eat(string food);
    string GetDescription();
}
