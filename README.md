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

| Screen | What it does |
| --- | --- |
| **Main** (`UserFaceDetector`) | Shows the live camera feed. **Capture** takes the current frame and reports whether the person is verified or anonymous. **Restart** clears the result and resumes the feed. |
| **Add user** (`AddUser`) | Captures a frame and saves it to `CapturedImages/` as a registered user image. |
| **Images** (`Images`) | Lists every registered image, with a delete button for each. |

Use the **Camera** dropdown to pick a different capture device if more than one
is connected.

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
├── UserFaceDetector.cs      main form: camera feed and verification
├── AddUser.cs               registers a new user image
├── Images.cs                lists registered images
├── Image.cs                 user control for one image row
├── FaceRecognizer.cs        face detection and embedding comparison
├── AppPaths.cs              resolves the image folder locations
├── AnonymousImages/         the most recent captured frame
└── CapturedImages/          registered user images
```

Image paths are resolved in one place, [`AppPaths`](PCUserDetection/AppPaths.cs),
from the folder the executable lives in. Both folders are created on demand if
they are missing.

## Notes

- Recognition used to live in a separate `FaceDetection` console app that the
  form launched as a child process and parsed stdout from. It now runs
  in-process, which removed the hardcoded path to the console executable and
  avoids reloading the ONNX models on every capture.
- A running instance locks `bin\Debug\PCUserDetection.exe`, so close the app
  before rebuilding.
