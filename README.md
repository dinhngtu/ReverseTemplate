# Introduction

Transform unstructured data into objects. Includes standalone library and PowerShell module.

# Building

Compatible with Windows PowerShell and PowerShell 6+ (including PowerShell on Linux).

```powershell
dotnet publish -c Release --no-self-contained "ReverseTemplate.PSModule/ReverseTemplate.PSModule.csproj" -o build
Import-Module ./build/ReverseTemplate.PSModule.dll
Import-TemplatedData -TemplatePath template.txt -RootPath data -Recurse
```

# Example template

template.txt:
```
#dir{{%d=dirno}}/file{{%d=fileno}}.txt
Your data is {{/[a-z]+/=data}}
```

data/dir1/file1.txt:
```
Your data is thefirstfile
```

data/dir1/file2.txt:
```
Your data is thesecondfile
```

Output:
```
data          dirno fileno
----          ----- ------
thefirstfile  1     1
thesecondfile 1     2
```

# Template format

* The program is based on the concept of **records**. Each record is an object resulting from the application of a template file to your data.
* One data file can contain one record, or multiple records (in Multiple mode).
* Each template describes one record, including its fixed and variable text. You retrieve information from your data using **captures**. Each capture takes the corresponding value in the data and assigns it to the corresponding property in the record.

See below for a detailed example of a template file:
```
This is a ReverseTemplate template file.
The template file is line-based; each template line is independently considered by the program.

Normal text like this are matched literally by the template engine.
They serve as "anchors" for your variable data in each data file.

To capture variable data, you use capture groups like so: My age is {{%d=age}}.
Each capture group consists of a pattern and a variable path surrounded in curly brackets.
In the above example, "%d" is the pattern and "age" is the variable path.
This means I'll capture an integer number and put it in the "age" property of my record.
Capture patterns also support .NET regular expressions {{/like/=this}}.
If you don't care about the data, you can write a capture group without a variable path: {{%d}}

Variable paths, like their name, can produce nested properties.
This is useful for creating {{%d=nested.objects}}
You can also create {{%d=nested.arrays[]}} as well.

For a full description of the template syntax, including advanced capture patterns and flags,
please see the included source code.
```

Aside from text-based matching, ReverseTemplate supports advanced directives:
* Filename matching: must be the first line of your template file
```
#relative/path/to/{{%w=file}}/can/be/{{%w=captured}}
```
* Filters: find/replace before matching data, useful for cleaning up errors or useless information
```
#/replace this pattern/with this text
```

ReverseTemplate is based on regular expressions. Beware of complexity blowup!

# License notice

```
ReverseTemplate
Copyright (C) 2020 Tu Dinh

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, version 3.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
```
