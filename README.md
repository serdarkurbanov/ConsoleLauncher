## Download

Release version is available from [release](https://github.com/serdarkurbanov/ConsoleLauncher/releases) section of the repo: [zipped binaries](https://github.com/serdarkurbanov/ConsoleLauncher/releases/download/1.0/ConsoleLauncher_1.0.0.zip)

## About Console Launcher

Console Launcher is a simple console manager written in C# (WPF). This implies it will work for Windows, unless ported with Mono. As for now, the project is targeting .NET 4.5.2

The UI is built using the Metro style provided by MahApps (http://mahapps.com).

![how it looks](https://cloud.githubusercontent.com/assets/10394306/25688824/4aae55a2-3050-11e7-9bd8-19965e373e68.png)

## What it can do

The main thing this application provides is displaying the console output of running programs. Folders and running tasks are displayed in the tree in the left hand side.

The console manager can be useful in the case of managing large number of processes that need to be started from console.

Plots of CPU, memory and thread usage provide additional diagnostics. Plots are made using plotting library OxyPlot (https://github.com/oxyplot). Since the plot building can consume system resources, the program allows to switch off the resource tracking by clicking the switch at the right upper corner (switched off by default).

![how it looks with the plots](https://cloud.githubusercontent.com/assets/10394306/25688857/88e51b80-3050-11e7-91aa-c8f5b4dea20c.png)

## Working with the program

The UI is pretty intuitive: adding the folder/process to the tree is made with the context menu of the tree. You can start the selected process by hitting 'return', by hitting a 'play' button at the top of the window or by selecting the relevant menu iem from the context menu.

You can start all the processes for the selected folder in the same way.

To stop the selected process you can press 'CTRL+C', hit the stop button at the top or select 'stop' command from the context menu.

The list of records from the console output of the running program supports multiple selection (with ctrl, shift modifiers) and copy operation (triggered by 'CTRL+C').

'Edit' command from the context menu opens the following window (same window for 'add new'). Note that the name of the program displayed in the tree can be different from the command that you run
![consolelauncher_program](https://cloud.githubusercontent.com/assets/10394306/25365102/9cdd4ac2-2934-11e7-9aa3-9fbd65b686f7.png)

You can run a sequence of commands in a way you would run it from Windows command line. 
For example: `ping google.com & ping github.com`

## Saving the changes

You don't need to do any specific action to save changes you made - the application saves everything automatically on every change (adding/deleting a folder etc.). The settings are written to the file **launcher_config.json** which you can find in the application folder (Newtonsoft.Json is used for reading and writing the settings file, see http://www.newtonsoft.com/json).
