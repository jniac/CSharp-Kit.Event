using System;
using System.Collections.Generic;

namespace MySuperProgram
{
    class Foo
    {
        static int counter = 0;

        public readonly int id;
        public string name;
        public Foo parent;
        public List<Foo> children = new List<Foo>();

        public Foo(string name = "no name")
        {
            id = counter++;
            this.name = name;
        }

        public void addChild(Foo child)
        {
            child.parent = this;
            children.Add(child);
        }

        public void removeChild(Foo child)
        {
            if (children.Contains(child))
            {
                child.parent = null;
                children.Remove(child);
            }
        }

        public override string ToString()
        {
            return $"Foo#{id} \"{name}\"";
        }
    }

    class MySpecialEvent : Kit.Event
    {
        public float number = 3;
    }

    class Program
    {
        static void ConsoleTitle(string title)
        {
            Console.WriteLine($"\n===========\n{title}:\n-----------");
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Foo foo = new Foo { name = "fooooo" };
            foo.addChild(new Foo { name = "foo-1" });
            foo.addChild(new Foo { name = "foo-2" });
            foo.children[1].addChild(new Foo { name = "foo-2-a" });
            foo.children[1].addChild(new Foo { name = "foo-2-b" });

            Foo fooChild = foo.children[1].children[0];



            ConsoleTitle("basic test");

            Kit.Event.On<MySpecialEvent>(foo, "*",
                e => Console.WriteLine($"top({foo.name}): " + e.ToLongString()), key: "basic test");

            Kit.Event.On<MySpecialEvent>(fooChild, "*",
                e => Console.WriteLine($"bottom({fooChild.name}): " + e.ToLongString()), key: "basic test");

            Kit.Event.Dispatch(new MySpecialEvent { Target = foo, type = "hey" });

            Kit.Event.Dispatch(new MySpecialEvent
            {
                Target = fooChild,
                type = "ascending I",
                propagation = obj => (obj as Foo).parent,
                number = MathF.E,
            });

            Kit.Event.Dispatch(new MySpecialEvent
            {
                Target = foo,
                type = "descending I",
                propagation = obj => (obj as Foo).children,
                number = MathF.PI,
            });





            ConsoleTitle("Cancel() test");

            Kit.Event.On<MySpecialEvent>(foo.children[1], "*", e => {
                if (e.Cancel())
                    Console.WriteLine($"event \"{e.type}\" canceled!!!!");
                else
                    Console.WriteLine($"event \"{e.type}\" NOOOOT canceled!");
            });

            Kit.Event.Dispatch(new MySpecialEvent
            {
                Target = fooChild,
                type = "ascending II",
                propagation = obj => (obj as Foo).parent,
            });

            Kit.Event.Dispatch(new MySpecialEvent
            {
                Target = foo,
                type = "descending II",
                propagation = obj => (obj as Foo).children,
            });





            ConsoleTitle("Cancelable (= false) test");

            Kit.Event.Dispatch(new MySpecialEvent
            {
                Target = fooChild,
                type = "ascending III (uncancelable)",
                Cancelable = false,
                propagation = obj => (obj as Foo).parent,
            });


            // off
            Kit.Event.Off(foo);
            Kit.Event.Off(fooChild);





            ConsoleTitle("global test");

            Kit.Event.On<MySpecialEvent>(e => {
                Console.WriteLine($"global listener @\"{e.type}\" ({e.number})");
            }, key: "yolo");
            Kit.Event.Dispatch(new MySpecialEvent { number = MathF.PI * 2 });
            Kit.Event.Dispatch(new MySpecialEvent { number = MathF.PI * 3 });
            Kit.Event.Off(key: "yolo");
            Kit.Event.Dispatch(new MySpecialEvent { number = MathF.PI * 4 });





            ConsoleTitle("AlsoGlobal test");

            Kit.Event.On<MySpecialEvent>(e => {
                Console.WriteLine($"also global listener {e.ToLongString()}");
                e.number++;
            }, key: "yolo");
            Kit.Event.Dispatch(new MySpecialEvent
            {
                Target = fooChild,
                AlsoGlobal = true,
                number = 12,
                propagation = obj => (obj as Foo).parent,
                Cancelable = false,
            });
        }
    }
}
