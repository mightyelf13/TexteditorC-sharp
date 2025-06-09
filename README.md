# TexteditorC#


Welcome to my text editor

### INSTALLATION

Using C# open the folder in your prefered text editor and run it from the text editor and the program should run fine

### USER MANUAL 

##### menu items

- open: opening a text file functionality
- save: saving the current text file 
- quit: quits the program using a button
- encoding: choosing a specific encoding can change your current encoding to the one chosen

### OVERVIEW

#### GUI ELEMENTS

the GUI is seperated into 4 elements 
- menu bar
- tool bar
- text view
- status bar

all of these boxes(frames)/widgets are incorperated into one vertical box (frame) class by gtk to to organize the GUI

###### menu bar
the menubar is created using the "MenuItem" class in GTK with three main menus

###### tool bar
this is a remanent from when I wanted to make the text editor a rich text editor and add other tools, but for now it has one functionality it is built using a the 
"Toolbar" class in GTK 

###### Text View
The textview widget is the main component of the program, its divided into two boxes(frames) using the horizontal box class, a box for line numbers and a box for the 
"textview" widget by gtk, both these boxes are then added into the main box

###### status bar
The most intresting in my opinion, made using a box class imitating a toolbar class, and incmopassing the most improtant functions of the program
the widgets in it are added together using a horizontal box like the one used in the textview

#### Functions and Backend

The Program isnt too complicated, built it as simple as possible where each function of the program is connected to an event either through clicking a button or a 
keyboard shortcut.
The functions are organized in way that it moves incrementally with the GUI elements where at first you have the onsave(), onopen(), onquit() functions then the 
encoding functions etc 

and at the end of the functions section you can see the keyboard shortcuts functions 

in the following paragraphs there will be a brief summary of the technicalities of the functions

###### file operations

the open and save functions use the FileChooserDialog class by gtk to communicate with the operating ssytem files and both use the filefilter class by gtk to limit the 
files in the file chooser to the .txt file format only

###### Enconding functions

the encoding functions are pretty simple, there is one global variable for encoding and the each function corresponds to the selected (toggled?) encoding type in the 
menuitems and then changes it in the label in the status bar and the onsave function (also changes when onopen inputs a new type)

###### search & replace

the most complicated functions in my opinion and the most i am proud of. using the textbuffer and texttag classes the gtk provides it looks through the textview 
characters and tags the string inputed

and the replace funciton is rather simple where it takes the last searched string and using the system regular expression function replaces the strings 

###### line operations

using a shortcut event set up in the gui above the functions are called, Using the TextIter function by gtk it gets the cursor location then finds the line number and 
selects the start and end of it and deletes it, and does the same for the copy function but instead of deleting it adds it to the clipboard


### disclaimer

the undo and redo functions are abandoned functions and are just there as proof of concept where the undo functions stores every change so it takes alot of memory , in 
production verion :) this will be removed 


also i should be able to provide a image/drawing of the gui elements and functions (not sure)
