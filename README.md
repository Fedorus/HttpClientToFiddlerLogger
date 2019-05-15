# HttpClientToFiddlerLogger

Log HttpClient requests to fiddler`s SAZ format.

[![httpclienttofiddlerlogger MyGet Build Status](https://www.myget.org/BuildSource/Badge/httpclienttofiddlerlogger?identifier=82af49a5-68ed-4388-94d8-ac3ed7919d1d)](https://www.myget.org/)

# Example

```C#
static async Task ShortExample()
{
    using (var logger = new FiddlerLogger(new HttpClientHandler(), "logfileName.saz"))
    {
        var client = new HttpClient(logger);
        await client.GetAsync("example.com");
    }
}
```
Notice that FiddlerLogger object should be disposed or Saved() in order
to actually save logs in file.
