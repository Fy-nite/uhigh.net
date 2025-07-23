class Program {
  cats = new Observable<string>("meow", false);
  Main(args) {
    cats.Subscribe((cat) => {
      if (cat === "hiss") {
        Console.WriteLine("The cat is hissing. stay back!");
      } else {
        if (cat === "meow") {
          Console.WriteLine("The cat is meowing. It wants attention!");
        } else {
          if (cat === "purr") {
            Console.WriteLine("The cat is purring. It's happy!");
          } else {
            Console.WriteLine("Unknown cat sound: " + cat);
          }
        }
      }
    });
    cats.Add("purr");
    cats.Add("meow");
    cats.Add("hiss");
  }
}

