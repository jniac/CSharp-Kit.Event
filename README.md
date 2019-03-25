# CSharp-Kit.Event

a very, very, very versatile event manager for C# (for a fonctional programing inspired by (best?) javascript practices).

[the file](./Kit.Event/Kit/Event.cs)

```csharp

// let's declare a brand new event

class MySpecialEvent : Kit.Event
{
    public int count = 0;
}



// elsewhere, in the program, about a certain "node"...

Kit.Event.On<MySpecialEvent>(e => {

    e.count++;
    
    Console.WriteLine(
        "MySpecialEvent has been dispatched:" +
        $"\n  current target is {e.Target}" +
        $"\n  origin target is {e.OriginTarget}" +
        $"\n  count = {count}"
    );
    
}, key: "yolo");

Kit.Event.Dispatch(new MySpecialEvent {

    Target = node,
    AlsoGlobal = true,
    propagation = obj => (obj as Node).children,
  
});

Kit.Event.Off(key: "yolo");

```
