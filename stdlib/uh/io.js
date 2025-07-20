class IO {
  Print(message) {
    _print(message);
  }
  Print(values) {
    let result = "";
    for (let i = 0; i < values.length; ++i) {
      if (i > 0) {
        result += " ";
      }
      result += ToString_of(values[i]);
    }
    Print(result);
  }
  Input() {
    _ReadLine();
  }
  Input(prompt) {
    Print(prompt);
  }
  ReadAllText(filePath) {
    _ReadFile(filepath);
  }
  WriteAllText(filePath, contents) {
    _WriteToFile(filePath, contents);
  }
}

