namespace GitTransformer;

public static class Constants
{
    public const string SnippetTemplate = """
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
                "Shortcut": null,
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
                          "ID": null,
                          "ToolTip": null,
                          "Default": null
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
        """,
        SqlJsonFormat = """
        {
          "prefix": "TableAndDetails",
          "description": "Query Table and TableDetails",
          "body": [
        	"DECLARE @Id INT = ${1:Id}",
        	"SELECT * FROM ${2:Database}.${3:dbo}.[${4:Table}] WHERE [Id] = @Id",
        	"SELECT * FROM ${2:Database}.${3:dbo}.[${5:TableDetails}] WHERE [Id] = @Id",
        	"ORDER BY [${6:DateAdded}] DESC"
          ]
        }
        """;
}