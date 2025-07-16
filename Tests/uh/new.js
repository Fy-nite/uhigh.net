

class Program {
  constructor() {
    this.cats = new Observable("meow", false);
    this.cats.Subscribe((cat) => {
      if (cat === "hiss") {
        console.log("The cat is hissing. stay back!");
      } else {
        if (cat === "meow") {
          console.log("The cat is meowing. It wants attention!");
        } else {
          if (cat === "purr") {
            console.log("The cat is purring. It's happy!");
          } else {
            console.log("Unknown cat sound: " + cat);
          }
        }
      }
    });
    this.cats.Add("purr");
    this.cats.Add("meow");
    this.cats.Add("hiss");
    cats.Add("purr");
    cats.Add("meow");
    cats.Add("hiss");
  }
}

