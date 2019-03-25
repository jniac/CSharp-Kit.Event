using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Kit
{
    public class Listener
    {
        public static Dictionary<object, List<Listener>> dict = new Dictionary<object, List<Listener>>();
        public static object groups = new { name = "ListenerGroupTarget" };

        public static List<Listener> Retrieve(object target)
        {
            return dict.ContainsKey(target) ? dict[target] : null;
        }

        private static List<Listener> Add(object target, Listener listener)
        {
            List<Listener> listeners = Retrieve(target);

            if (listeners == null)
                dict[target] = listeners = new List<Listener>();

            listeners.Add(listener);

            return listeners;
        }

        private static void Remove(object target, Listener listener)
        {
            dict[target].Remove(listener);

            if (dict[target].Count == 0)
                dict.Remove(target);
        }

        public static void Clear(object target)
        {
            if (dict[target] != null)
            {
                dict[target].Clear();
                dict.Remove(target);
            }
        }

        public static Listener GetGroupListener()
        {
            return new Listener(groups, "none", null);
        }




        static int counter = 0;

        public readonly int id;
        public readonly object target;
        public readonly string type;
        public readonly object key;
        public readonly Action<Event> callback;

        public Listener(object target, string type, Action<Event> callback, object key = null)
        {
            id = counter++;

            Listener.Add(target, this);

            this.target = target;
            this.type = type;
            this.callback = callback;
            this.key = key;
        }

        private Listener parent;
        private List<Listener> children;
        internal void AddChild(Listener child)
        {
            if (children == null)
                children = new List<Listener>();

            child.parent = this;
            children.Add(this);
        }

        public void Destroy()
        {
            if (parent != null)
            {
                parent.children.Remove(this);
            }

            if (children != null)
            {
                foreach (Listener child in children)
                    Listener.Remove(child.target, child);
            }

            Listener.Remove(target, this);
        }

        public bool Match(string type = "*", object key = null, Action<Event> callback = null)
        {
            return (this.type == "*" || type == "*" || type == this.type)
                && (key == null || key == this.key)
                && (callback == null || callback == this.callback);
        }

        public override string ToString()
        {
            return string.Format("Listener#{0}[type: {1}, key: {2}]", id, type, key);
        }
    }



    // Events:

    public class Event
    {
        public static object global = new { name = "GlobalTarget" };

        public delegate object PropagationCallback(object gameObject);

        public static Listener On(object target, string type, Action<Event> callback, object key = null)
        {
            if (target is IList)
            {
                Listener groupListener = Listener.GetGroupListener();

                foreach (object item in (IList)target)
                    groupListener.AddChild(On(item, type, callback, key));

                return groupListener;
            }

            return new Listener(target, type, callback, key);
        }

        // generic
        public static Listener On<T>(object target, string type, Action<T> callback, object key = null) where T : Event
        { return On(target, type, e => callback(e as T), key); }

        // short for a global Listener
        public static Listener On(string type, Action<Event> callback, object key = null)
        { return On(Event.global, type, callback, key); }

        public static Listener On<T>(string type, Action<T> callback, object key = null) where T : Event 
        { return On<T>(Event.global, type, callback, key); }

        public static Listener On<T>(Action<T> callback, object key = null) where T : Event
        { return On<T>(Event.global, "*", callback, key); }



        static int onceKeyCounter = 0;
        public static void Once(object target, string type, Action<Event> callback, object key = null)
        {
            string onceKey = "EventOnceKey-" + onceKeyCounter++;

            Action<Event> onceCallback = e =>
            {
                Off(target, type, key: onceKey);

                callback(e);
            };

            On(target, type, onceCallback, onceKey);
        }

        public static void Off(object target, string type = "*", Action<Event> callback = null, object key = null)
        {
            if (target is IList)
            {
                foreach (object item in (IList)target)
                    Off(item, type, callback, key);

                return;
            }

            List<Listener> listeners = Listener.Retrieve(target);

            if (listeners == null)
                return;

            foreach (Listener listener in listeners.ToArray())
            {
                if (listener.Match(type, key, callback))
                    listeners.Remove(listener);
            }

            if (listeners.Count == 0)
                Listener.Clear(target);
        }

        public static void Off(string type = "*", Action<Event> callback = null, object key = null)
        { Off(Event.global, type, callback, key); }


        public static void Dispatch(Event e)
        {
            e.Locked = true;

            if (e.AlsoGlobal && e.target != Event.global) // do not invoke "global" twice
            {
                List<Listener> globalListeners = Listener.Retrieve(Event.global);

                if (globalListeners != null)
                {
                    foreach (Listener listener in globalListeners.ToArray())
                    {
                        if (listener.Match(e.type))
                            listener.callback(e);

                        if (e.Canceled)
                            return;
                    }
                }
            }

            List<Listener> listeners = Listener.Retrieve(e.target);

            if (listeners != null)
            {
                foreach (Listener listener in listeners.ToArray())
                {
                    if (listener.Match(e.type))
                        listener.callback(e);

                    if (e.Canceled)
                        return;
                }
            }

            e.DoPropagation();
        }

        //public static void Dispatch(object target, string type, bool cancelable = true, PropagationCallback propagation = null)
        //{
        //    Dispatch(new Event { 
        //        target = target,
        //        originTarget = target,
        //        type = type,
        //        cancelable = cancelable,
        //        propagation = propagation,
        //    });
        //}

        //// global shorthands
        //public static void Dispatch(string type, bool cancelable = true, PropagationCallback propagation = null)
        //{ Dispatch(Event.global, type, cancelable, propagation); }

        //public static void Dispatch(bool cancelable = true, PropagationCallback propagation = null)
        //{ Dispatch(Event.global, "*", cancelable, propagation); }





        // instance:
        public bool Locked { get; private set; } = false;

        public string type = "*";

        protected object target = Event.global;
        public object Target
        {
            get { return target; }
            set { if (Locked == false) target = value; }
        }

        protected object originTarget;
        public object OriginTarget { get { return originTarget ?? target; } }

        public bool AlsoGlobal { get; set; } = false;

        public PropagationCallback propagation;

        protected bool cancelable = true;
        public bool Cancelable 
        {
            get { return cancelable; } 
            set { if (Locked == false) cancelable = value; }
        }
        public bool Canceled { get; private set; }

        public bool Cancel()
        {
            Canceled |= Cancelable;

            return cancelable;
        }



        virtual protected Event Clone(object newTarget)
        {
            Type eventType = GetType();
            Event clone = Activator.CreateInstance(eventType) as Event;

            foreach (FieldInfo field in eventType.GetFields())
                field.SetValue(clone, field.GetValue(this));

            foreach(PropertyInfo property in eventType.GetProperties())
                if (property.CanWrite)
                    property.SetValue(clone, property.GetValue(this));

            clone.target = newTarget;
            clone.originTarget = OriginTarget;
            clone.AlsoGlobal = AlsoGlobal;
            clone.Cancelable = Cancelable;

            return clone;
        }

        private void DoPropagation()
        {
            if (propagation == null)
                return;

            object newTarget = propagation(target);

            if (newTarget == null)
                return;

            if (newTarget is IList)
                foreach (object obj in (newTarget as IList))
                    Dispatch(Clone(obj));

            else
                Dispatch(Clone(newTarget));
        }

        private static Regex reToLongString = new Regex(@"^type|propagation$");
        public string ToLongString()
        {
            string[] fields = GetType()
                .GetFields()
                .Where(fi => reToLongString.Match(fi.Name).Success == false)
                .Select(fi => fi.Name + ":" + fi.GetValue(this))
                .ToArray();

            return GetType().Name + ":" +
                "\n\t" + "type: \"" + type + "\"" +
                "\n\t" + "Locked: " + Locked +
                "\n\t" + "Target: " + Target +
                "\n\t" + "OriginTarget: " + OriginTarget +
                "\n\t" + "AlsoGlobal: " + AlsoGlobal +
                "\n\t" + "Cancelable: " + Cancelable +
                "\n\t" + "Canceled: " + Canceled +
                "\n\t" + "propagation: " + (propagation == null ? "no" : "yes") +
                "\n\t" + string.Join("\n\t", fields) +
                "";
        }

        public override string ToString()
        {
            return "Event type:" + type + " target: " + target + "";
        }
    }
}
