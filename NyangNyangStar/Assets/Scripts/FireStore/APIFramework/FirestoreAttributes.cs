using System;


[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class FirestorePathAttribute : Attribute
{
    public string Template { get; }
    public FirestorePathAttribute(string template) { Template = template; }
}


[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class FirestoreFieldAttribute : Attribute
{
    public string Key { get; }
    public FirestoreFieldAttribute(string key) { Key = key; }
}


[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class FirestoreIgnoreAttribute : Attribute { }



[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class FirestoreMapAttribute : Attribute { }


[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class FirestoreMapKeyAttribute : Attribute { }
