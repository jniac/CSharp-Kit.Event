# CSharp-Kit.Event

a very, very, very versatile event manager for C# (for a fonctional programing inspired by (best?) javascript practices).

```csharp

// let's declare a brand new event

class MySpecialEvent : Kit.Event
{
    public float count = 0;
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

    target = node,
    propagation = obj => (obj as Node).children,
    AlsoGlobal = true,
  
});

Kit.Event.Off(key: "yolo");

```