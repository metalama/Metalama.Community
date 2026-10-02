## How Virtuosity works

The following members are not virtualized:
* static members. Since C# 11, `static virtual` members are possible in interfaces, but they serve a different use case, so they are not virtualized either.
* private members, which can't be virtual
* abstract members, which are already implicitly virtual
* members in nested structs and interfaces
* indexers and events, which are not visited by the aspect

The `sealed` modifier is removed, both from the type and from its members. It is removed unconditionally, including
from members that are then not virtualized: a member explicitly declared `sealed override` keeps its `override` but
loses its `sealed`, and so becomes overridable again.

## Why is this useful

Maybe you're using one of the following tools:

 * [NHibernate](https://nhibernate.info/)
 * [Rhino Mocks](https://hibernatingrhinos.com/oss/rhino-mocks)
 * [Moq](https://github.com/moq/moq)
 * [Ninject](http://www.ninject.org/)
 * [NSubstitute](https://nsubstitute.github.io/)
 * [Entity Framework](https://docs.microsoft.com/en-us/ef/)

All these tools make use of [DynamicProxy](http://www.castleproject.org/projects/dynamicproxy/). DynamicProxy allows for runtime interception of members. The one caveat is that all intercepted members must be virtual. This means that that all the above tools, to some extent, require members to be virtual:

 * [EF: Lazy Loading](https://docs.microsoft.com/en-us/ef/ef6/querying/related-data#lazy-loading) "When using POCO entity types, lazy loading is achieved by creating instances of derived proxy types and then overriding virtual properties to add the loading hook"
 * [Must Everything Be Virtual With NHibernate?](http://davybrion.com/blog/2009/03/must-everything-be-virtual-with-nhibernate/)
 * [RhinoMocks: Why methods need to be declared virtual](http://groups.google.com/group/RhinoMocks/browse_thread/thread/a2cb93f1ba8d4735/37d377ddb92cb729?lnk=gst&q=virtual)
 * [Moq: My method needs to be virtual?](http://groups.google.com/group/moqdisc/browse_thread/thread/2e02e367d017f274)
 * ["NMock supports mocking of classes with virtual methods"](http://www.nmock.org/nmock1-documentation.html)
 * [Ninject Important Note:Your methods/properties to be intercepted must be virtual!](http://innovatian.com/2010/03/using-ninject-extensions-interception-part-1-the-basics/)
 * [NSubstitute:Some operations on non virtual members should throw an exception](http://groups.google.com/group/nsubstitute/browse_thread/thread/407cb0408ce97bfd)

When a member is not virtual these tools will not work and fail in sometimes very unhelpful ways. So rather than having to remember to use the virtual keyword, Virtuosity means members will be virtual by default.

You may also just prefer to work with all methods virtual by default, as in Java.