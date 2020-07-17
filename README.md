# Unity Plane Renderer
Unity component that simplifies rendering images in 3D space.

## Features
 - Automatically scales images to avoid stretching
 - Set different images for front and back of plane
 - Share one material between multiple PlaneRenderers without conflict
 - Supports transparent materials and shadows
 - Updates automatically when editor values are changed

## How To Use
Attach the PlaneRenderer script to the object that you want to add an image to. Two child objects will be created--one for the front of the image and one for the back. Do not edit these objects directly, or add or remove children from the object with the PlaneRenderer attached. The PlaneRenderer will give you the following options:
 - Front Image: The texture to use for the front of the plane (facing along the local Z axis)
 - Back Image: The texture to use for the back of the plane (facing against the local Z axis)
 - Mirror Back Image: Should the texture for the back image be mirrored? (this is useful if the front and back images are the same and you want them to line up)
  - Pixels Per Meter: How many pixels in the image are equivalent to one meter in Unity?
  - Anchor Point: Location of the image's transform anchor, where (0,0) is in the bottom left corner and (1,1) is the top right
  - Material: The material to use for rendering the image. If this field is null, nothing will display. The plane renderer copies its material before using it, so materials can be shared between PlaneRenderers and other objects without issue.
  - Offset: How far should the front and back planes be offset from the center? This is useful for preventing lighting errors if you want the PlaneRenderer to cast shadows
  - Cast Shadows: How should shadows be handled?
  
## Installation
Download or clone this repository and add it to your Unity project assets folder.

## Dependencies
This library requires that you have the [UnityEditorAttributes](https://github.com/dninosores/UnityEditorAttributes) library installed.
