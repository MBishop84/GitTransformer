namespace GitTransformer;

public static class Constants
{
    public const string JsonTemplate = """
        {
          "CodeSnippets": {
            "@xmlns": "http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet",
            "_locDefinition": {
              "@xmlns": "urn:locstudio",
              "_locDefault": {
                "@_loc": "locNone"
              },
              "_locTag": [
                {
                  "@_loc": "locData",
                  "#text": "Title"
                },
                {
                  "@_loc": "locData",
                  "#text": "Description"
                },
                {
                  "@_loc": "locData",
                  "#text": "Author"
                },
                {
                  "@_loc": "locData",
                  "#text": "ToolTip"
                }
              ]
            },
            "CodeSnippet": {
              "@Format": "1.0.0",
              "Header": {
                "Title": "",
                "Shortcut": "",
                "Description": "",
                "Author": "https://mbishop84.github.io/GitTransformer/transformer",
                "SnippetTypes": {
                  "SnippetType": "Expansion"
                }
              },
              "Snippet": {
                "Declarations": {
                    "Literal": [
                        {
                          "ID": "",
                          "ToolTip": "",
                          "Default": ""
                        }
                    ]
                },
                "Code": {
                  "@Language": "SQL",
                  "#cdata-section": ""
                }
              }
            }
          }
        }
        """;
}