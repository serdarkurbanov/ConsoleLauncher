## About Console Launcher

Console Launcher is a simple console manager for written in C# (WPF). This implies it will work for Windows, unless ported with Mono. As for now, the project is targeting .NET 4.5.2

The UI is build using the Metro style provided by MahApps (http://mahapps.com).

![how it looks](https://cloud.githubusercontent.com/assets/10394306/25364447/85184e90-2930-11e7-9978-9aec3f6ac8f4.png)

## What it can do

The main thing this application provides is displaying the console output of running programs. Folders and running tasks are displayed in the tree in the left hand side.

The console manager can be useful in the case of managing large number of processes that need to be started from console.

Plots of CPU, memory and thread usage provide additional diagnostics (under development). Plots are made using plotting library OxyPlot (https://github.com/oxyplot).

![how it looks with the plots](https://cloud.githubusercontent.com/assets/10394306/25364807/f53c1c40-2932-11e7-9780-b9d3b6115032.png)
