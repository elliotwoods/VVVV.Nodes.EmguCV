Introduction
============
https://github.com/elliotwoods/VVVV.Nodes.EmguCV

Simple video playback / video capture using EmguCV in VVVV
Options are open for full replacement of DirectShow, including spreadable video playback, CV manipulation, access to images in dynamic plugins, CV in dynamic plugins

This is designed for general use of OpenCV functions + also video playback / capture.

License
=======
This plugin is distributed under the LGPL license (as much as it can be)

Since we wrap EmguCV, currently we inherit the GPL license from EmguCV.

EmguCV is dual license (GPL/commercial). So this code is GPL, and you should be wary that also your code must be GPL if it employs this.
Suggest move to opencvsharp http://code.google.com/p/opencvsharp/
or discussion with EmguCV about a licensing model for the VVVV community. 
Ensure that all the OpenCV dll's + Emgu.CV.dll and Emgu.Util.dll are in the same folder as the plugin. Check Prerequisites.zip (there's an old version of the plugins in there as well)

Operating notes
===============

Video playback
--------------
This isn't necessarilly the best route for video playback, but can also work quite nicely.

Notes for video playback:
* This isn't going to employ any fancy hardware optimisation. If you need to chunk big video files, suggest sticking with FileStream + clever codecs.
* No audio, probably never will have (although other nodes which output CVImage type could give audio).
* Works with pretty much all the AVI's i threw at it (which isn't that many on this PC). Feedback welcome!

Types
-----
Currently we're moving to CVImage (rather than seperate types for each format: Image16L, ImageRGB, etc)
CVImage wraps an EmguCV IImage
It can be used for lots of things. We can make nodes in minutes to supply video from lots of sources (e.g. CLEye, Point Grey, BlackMagic) and then use them with the full chain of CV / texture utils.


Threading
---------
Current design is 'Highly Threaded' or 'Background' as described in the threading options list at
http://vvvv.org/forum/replacing-directshow-with-managed-opencv.-video-playback-capture-cv

Each node has it's own thread, every exchange of image data between threads results in a double buffer
This obviously leads to many threads + much memory usage (perhaps in the future users will be able to select the global threading model at runtime)

The node thread has an output buffer (single).
When the thread is ready to send (i.e. all of its inputs are fresh) then it calls FOutputBuffer.SetImage(...) which pushes the image downstream.
FOutput should not have any internal buffers, instead passing through its input directly

Links are double buffers


Memory usage
------------

Examples of memory usage:
# 640*480 ~= 300KB (VGA mono)
# 640*480*3 ~= 1MB (VGA colour)
# 640*480*16 ~= 5MB (VGA colour + alpha, 32bit float)
# 1920*1080*3 ~= 6MB (HD colour video frame)

Generally each slice at each node = 2 * the above (double buffered)

Todo
====

Interfaces
----------
ICaptureNode
ICaptureInstance
IFilterNode
IFilterInstance

Node suggestions
================

General
-------
* +						adds colour values
* -						subtracts colour values
* Queue
* Cons

Files
-----
* ImageLoad				Loads a set of images into RAM as ImageRGB's. Either use OpenCV's image loader or .NET's Bitmap class (probably quicker)
* ImageSave
* VideoSave				Built into CV so should be decent performance

Tracking
--------
* Contour

CameraCalibration
-----------------
* StereoCalibrate

Future
------
* AsImage				Convert texture to image (AsVideo in existing DirectShow). - requires texture input on plugins (i.e. not possible yet)