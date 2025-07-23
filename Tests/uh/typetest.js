class Program {
    static Main(args) {
        const stringMeow = new Meow("Hello");
        const result = stringMeow.GetValue();
        console.log(result);
    }
}

// Generic class: Meow<T>
class Meow {
    constructor(value) {
        this.value = value;
    }

    GetValue() {
        return this.value;
    }
}
console.log(stringMeow.GetValue()); // "Hello"
console.log(numberMeow.GetValue()); // 42

