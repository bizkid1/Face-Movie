using System;
using FaceRecognitionDotNet;
using FaceRecognitionDotNet.Extensions;
using OpenCvSharp;
using System.Linq;
using System.Drawing;

namespace Namespace
{
    public class Module
    {
        private static FaceRecognition _FaceRecognition;
        public object DetectFace(string file) {
            // load image and find face locations.
            var image = FaceRecognition.LoadImageFile(file);
            var face_locations2 = _FaceRecognition.FaceLocations(image, 0, Model.Cnn).ToArray();
            var face_features = _FaceRecognition.FaceLandmark(image, face_locations2);
            // [
            //   {
            //   'box': [544, 166, 218, 289],
            //   'confidence': 0.9995384216308594,
            //   'keypoints': {'left_eye': (606, 270), 'right_eye': (706, 275), 'nose': (641, 328), 'mouth_left': (603, 386), 'mouth_right': (684, 389)}
            //   }
            // ]
            Console.WriteLine(face_locations2);
            if (face_locations2.Count() == 0) {
                return null;
            }

            //Let's find and angle of the face. First calculate 
            //the center of left and right eye by using eye landmarks.

            var leftEyeCenter = face_features.First()[FacePart.LeftEye];
            var rightEyeCenter = face_features.First()[FacePart.RightEye];
            // draw the circle at centers and line connecting to them
            var x = 0; 
            var y = 0;
            var w = 0;
            var h = 0;
            (x, y, w, h) = (face_locations2.First().Left + face_locations2.First().Right, face_locations2.First().Top + face_locations2.First().Bottom, face_locations2.First().Left + face_locations2.First().Right, face_locations2.First().Top + face_locations2.First().Bottom);
            var x2 = 0;
            var y2 = 0;
            (x2, y2) = (x + w, y + h);
            // Cv2.rectangle(image, (x, y), (x + w, y + h), (255, 0, 0), 2)
            // Cv2.circle(image, leftEyeCenter, 2, (255, 0, 0), 10)
            // Cv2.circle(image, rightEyeCenter, 2, (255, 0, 0), 10)
            // Cv2.line(image, leftEyeCenter, rightEyeCenter, (255, 0, 0), 10)
            // find and angle of line by using slop of the line.
            var dY = rightEyeCenter.First().Point.Y - leftEyeCenter.First().Point.Y;
            var dX = rightEyeCenter.First().Point.X - leftEyeCenter.First().Point.X;
            var angle = ConvertRadiansToDegrees(Math.Atan2(dY, dX));
            // to get the face at the center of the image,
            // set desired left eye location. Right eye location
            // will be found out by using left eye location.
            // this location is in percentage.
            var desiredLeftEye = (0.35, 0.35);
            // Set the croped image(face) size after rotaion.
            var desiredFaceWidth = 128;
            var desiredFaceHeight = 128;
            (desiredFaceWidth, desiredFaceHeight) = (image.Width, image.Height);
            var desiredRightEyeX = 1.0 - desiredLeftEye.Item1;
            // determine the scale of the new resulting image by taking
            // the ratio of the distance between eyes in the *current*
            // image to the ratio of distance between eyes in the
            // *desired* image
            var dist = Math.Sqrt(Math.Pow(dX, 2) + Math.Pow(dY, 2));
            var desiredDist = desiredRightEyeX - desiredLeftEye.Item1;
            // desiredDist *= desiredFaceWidth
            desiredDist *= 300;
            var scale = desiredDist / dist;
            // scale = 1
            // compute center (x, y)-coordinates (i.e., the median point)
            // between the two eyes in the input image
            var eyesCenter = new Point2f((leftEyeCenter.First().Point.X + rightEyeCenter.First().Point.X) / 2, (leftEyeCenter.First().Point.Y + rightEyeCenter.First().Point.Y) / 2);
            // grab the rotation matrix for rotating and scaling the face
            var M = Cv2.GetRotationMatrix2D(eyesCenter, angle, scale);
            // update the translation component of the matrix
            var tX = desiredFaceWidth * 0.5;
            var tY = desiredFaceHeight * desiredLeftEye.Item1;
            M(0,2) += tX - eyesCenter.X;
            M(1,2) += tY - eyesCenter.Y;
            // apply the affine transformation
            (w, h) = (desiredFaceWidth, desiredFaceHeight);
            var output = Cv2.WarpAffine(image, M, (w, h), borderMode: Cv2.BORDER_CONSTANT, flags: Cv2.INTER_CUBIC);
            Console.WriteLine("Writing cropped image: c_" + file.name);
            var font = Cv2.FONT_HERSHEY_SIMPLEX;
            Cv2.PutText(output, file.name, (10, image.shape[0] - 10), font, 1, (255, 255, 255), 2, Cv2.LINE_AA);
            Cv2.ImWrite("payload/output/c_" + file.name, Cv2.CvtColor(output, Cv2.COLOR_RGB2BGR));
        }
        static Module() {
            DetectFace(file);
        }
        public static double ConvertRadiansToDegrees(double radians)
        {
            double degrees = (180 / Math.PI) * radians;
            return (degrees);
        }
    }
}
