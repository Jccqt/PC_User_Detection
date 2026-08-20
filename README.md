# PC_User_Detection

A Windows desktop app that checks whether the person sitting at the PC is a
registered user. It captures a frame from the webcam, generates a face
embedding for it, and compares that against the embeddings of every
registered user image. If any pair is similar enough, the user is verified.

## Requirements

- Windows (the app uses WinForms and DirectShow, so it is Windows-only)
- [.NET 8 SDK](https://dotnet.microsoft.com/download) or newer to build
- A webcam

The project targets `net8.0-windows` with `RollForward=LatestMajor`, so it also
runs on a machine that only has a newer major runtime installed.

## Getting started

```bash
git clone https://github.com/Jccqt/PC_User_Detection.git
cd PC_User_Detection
dotnet build PCUserDetection.sln
```

Then run the app:

```bash
dotnet run --project PCUserDetection/PCUserDetection.csproj
```

Running it from Visual Studio with F5, or by launching the built executable
directly, works the same way. The image folders are located relative to the
executable rather than the working directory, so it does not matter where the
app is started from.

The face recognition models are pulled in through the `FaceAiSharp.Bundle`
NuGet package and copied into the build output automatically. There is nothing
to download by hand.

## Using the app

Everything lives in one window. The rail on the left switches between three
screens, and the two camera screens share the same live feed.

| Screen | What it does |
| --- | --- |
| **Detect** | **Capture** takes the frame on screen and reports whether the person is verified or anonymous. **Retake** releases the frame and resumes the feed. |
| **Add user** | **Save photo** writes the frame on screen to `CapturedImages/` as a registered user image. |
| **Images** | Every registered image, with a **Remove** button on each. |

Use the camera dropdown in the top right to pick a different capture device if
more than one is connected. It is hidden on the Images screen, where the camera
is stopped.

## How it works

Face recognition runs in-process using [FaceAiSharp](https://github.com/georg-jung/FaceAiSharp),
which wraps two ONNX models: SCRFD for face detection with landmarks, and
ArcFace for generating embeddings.

[`FaceRecognizer`](PCUserDetection/FaceRecognizer.cs) does the work:

1. The captured frame is written to `AnonymousImages/Anonymous.jpeg`.
2. A face is detected in that frame, aligned using its landmarks, and turned
   into an embedding.
3. Each `.jpeg` in `CapturedImages/` gets the same treatment.
4. The dot product of the two embeddings is their cosine similarity. Anything
   at or above **0.42** counts as the same person.

An image with no detectable face is skipped rather than failing the whole
comparison. The models are loaded once and reused for every detection, since
loading them takes roughly a second.

## Project layout

```
PCUserDetection/
├── UserFaceDetector.cs      the window: navigation and the three screens
├── Theme.cs                 colours, fonts and button styles
├── CameraView.cs            the webcam feed and the frame it hands out
├── ImageCard.cs             one registered image in the gallery
├── FaceRecognizer.cs        face detection and embedding comparison
├── AppPaths.cs              resolves the image folder locations
├── AnonymousImages/         the most recent captured frame
└── CapturedImages/          registered user images
```

Image paths are resolved in one place, [`AppPaths`](PCUserDetection/AppPaths.cs),
from the folder the executable lives in. Both folders are created on demand if
they are missing.

## Notes

- The look is defined in one file, [`Theme.cs`](PCUserDetection/Theme.cs).
  Editing the colours there changes the whole app; nothing else hardcodes one.
- The screens are docked rather than positioned by pixel, so the window can be
  resized and the camera feed grows with it.
- Recognition used to live in a separate `FaceDetection` console app that the
  form launched as a child process and parsed stdout from. It now runs
  in-process, which removed the hardcoded path to the console executable and
  avoids reloading the ONNX models on every capture.
- A running instance locks `bin\Debug\PCUserDetection.exe`, so close the app
  before rebuilding.
