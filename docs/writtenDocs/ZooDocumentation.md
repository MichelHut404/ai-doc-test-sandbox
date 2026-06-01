# Zoo.cs

In this file the zoo is being managed. There are different methods that the zoo needs.

## Private Fields 

The file has 2 readonly private variables:

- `_name`: This string variable holds the name of the zoo.
- `_animals`: This is a list of `IAnimal` that holds the animals that are in the zoo.

## Methods

### `public Zoo(string name)`

In this constructor the name of the zoo is being set.

### `public void AddAnimal(IAnimal animal)`

This method adds an animal to the `_animals` list and prints a confirmation message.

### `public void OpenDeuren()`

This method makes it so the system prints certain messages. There is also a loop that loops through the `_animals` list and makes them do certain methods that are written in the `Animal` class.

### `public void SluitDeuren()`

This method prints a line to the console.

### `public static void Main(string[] args)`

This is the main method that is being called upon running the program. In this method the zoo is being created. Animals are being created and added to the zoo. The method also opens the zoo doors and closes them after.

