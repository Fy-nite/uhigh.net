using System;
using System.Collections.Generic;
using uhigh.StdLib;

namespace uhigh.StdLib.Examples
{
    /// <summary>
    /// Example data model for serialization demos
    /// </summary>
    public class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string Email { get; set; } = "";
        public DateTime BirthDate { get; set; }
        public List<string> Hobbies { get; set; } = new();
    }

    /// <summary>
    /// Example usage of the serialization utilities
    /// </summary>
    public static class SerializationExamples
    {
        public static void RunExamples()
        {
            // Create sample data
            var people = new List<Person>
            {
                new Person
                {
                    Name = "John Doe",
                    Age = 30,
                    Email = "john@example.com",
                    BirthDate = new DateTime(1993, 5, 15),
                    Hobbies = new List<string> { "Reading", "Gaming", "Hiking" }
                },
                new Person
                {
                    Name = "Jane Smith",
                    Age = 25,
                    Email = "jane@example.com",
                    BirthDate = new DateTime(1998, 8, 22),
                    Hobbies = new List<string> { "Photography", "Travel" }
                }
            };

            // JSON serialization
            Console.WriteLine("=== JSON Serialization ===");
            var json = people.ToJson();
            Console.WriteLine(json);
            
            var peopleFromJson = json.FromJson<List<Person>>();
            Console.WriteLine($"Loaded {peopleFromJson?.Count} people from JSON");

            // XML serialization
            Console.WriteLine("\n=== XML Serialization ===");
            var xml = people.ToXml();
            Console.WriteLine(xml);

            // CSV serialization
            Console.WriteLine("\n=== CSV Serialization ===");
            var csv = people.ToCsv();
            Console.WriteLine(csv);

            // YAML serialization
            Console.WriteLine("\n=== YAML Serialization ===");
            var yaml = Serializer.Serialize(people, SerializationFormat.Yaml);
            Console.WriteLine(yaml);

            // File operations
            Console.WriteLine("\n=== File Operations ===");
            DemonstrateFileOperations(people);

            // Deep cloning
            Console.WriteLine("\n=== Deep Cloning ===");
            var originalPerson = people[0];
            var clonedPerson = Serializer.DeepClone(originalPerson);
            
            Console.WriteLine($"Original: {originalPerson.Name}, Age: {originalPerson.Age}");
            Console.WriteLine($"Clone: {clonedPerson?.Name}, Age: {clonedPerson?.Age}");
            
            // Modify clone to prove it's independent
            if (clonedPerson != null)
            {
                clonedPerson.Age = 99;
                Console.WriteLine($"After modifying clone - Original: {originalPerson.Age}, Clone: {clonedPerson.Age}");
            }
        }

        private static async void DemonstrateFileOperations(List<Person> people)
        {
            try
            {
                // Save to different formats
                await Serializer.SaveToFileAsync(people, "people.json");
                await Serializer.SaveToFileAsync(people, "people.xml");
                await Serializer.SaveToFileAsync(people, "people.csv");

                Console.WriteLine("Files saved: people.json, people.xml, people.csv");

                // Load back from files
                var loadedFromJson = await Serializer.LoadFromFileAsync<List<Person>>("people.json");
                var loadedFromXml = await Serializer.LoadFromFileAsync<List<Person>>("people.xml");
                var loadedFromCsv = await Serializer.LoadFromFileAsync<List<Person>>("people.csv");

                Console.WriteLine($"Loaded from JSON: {loadedFromJson?.Count} people");
                Console.WriteLine($"Loaded from XML: {loadedFromXml?.Count} people");
                Console.WriteLine($"Loaded from CSV: {loadedFromCsv?.Count} people");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"File operation error: {ex.Message}");
            }
        }
    }
}
