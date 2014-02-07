# Ricochet RPC
Ricochet RPC is an RPC framework for C# focusing on speed and robustness.  It comes with a client for C#.  

It is released under the MIT license.

# Usage
The best way to figure out how to use this framework is to look at the `TestClient` and `TestServer` example projects.  The gist is below.

## Server

```
// We will publish this function finding the length of a string
public static int Length(string s) { return s.Length; }
...
var server = new Server(IPAddress.Any, 11000);
server.Register<string, int>("length", Length);
server.Start();
```

## Client

```
Client client = new Client("127.0.0.1", 11000);

int result;
if (!client.TryCall<string, string>("length", "foobar", out result)) {
    // failed due to timeout, broken connection, etc.
} else {
    // result should be set to 6 here
}
```