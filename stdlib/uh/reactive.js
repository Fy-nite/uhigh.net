// Generic class: Observable<T>
class Observable {
  value;
  _observers;
  constructor(initialValue) {
    value = initialValue;
    _observers = [];
  }
  GetValue() {
    return value;
  }
  Subscribe(observer) {
    _observers.push(observer);
  }
  Add(newValue) {
    value = newValue;
    for (const observer of _observers) {
      observer(value);
    }
  }
}

