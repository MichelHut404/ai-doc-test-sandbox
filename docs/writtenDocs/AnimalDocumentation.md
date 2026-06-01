# Animal

This class defines an animal. It implements the `IAnimal` interface and contains multiple properties, fields, and methods.

## Public Properties

- `string Name`: Defines the animal's name.
- `string Species`: Defines the animal's species.
- `int Age`: Defines the animal's age.
 
## Private Fields

- `string _sound`: What kind of sound the animal makes.
- `string _favoriteFood`: What the favorite food of the animal is.

## Methods

### `public Animal(string name, string species, int age, string sound, string favoriteFood)`

This is the constructor of the class that sets all the fields and properties.

### `public void MakeSound()`

This method prints the animal's sound to the console.

### `public void Eat(string food)`

This method contains an if/else statement. Depending on the food passed in, it prints a different message to the console. If the food matches the animal's favorite food it prints the eager response; otherwise it prints the indifferent response.

### `public string GetDescription()`

This method returns a string containing the name, species, and age of the animal.