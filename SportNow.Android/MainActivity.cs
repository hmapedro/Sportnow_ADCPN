﻿using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Android.Util;
using Android.Content;
using Xamarin.Forms;
using Plugin.FirebasePushNotification;
//Ausing Acr.UserDialogs;
using Plugin.CurrentActivity;
using System.Collections.Generic;
using SportNow.Views;
using Android.Content.Res;
using Plugin.DeviceOrientation;
using System.Net;
using Firebase;
using Android.Support.V4.App;
using Android;
using System.Threading.Tasks;
using System.IO;
using Android.Media;
using Android.Graphics;
using SportNow.Services.Camera;

namespace SportNow.Droid
{


    [Activity(Label = "My AKSL", Icon = "@mipmap/logo", Theme = "@style/MainTheme", LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]//, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {

        private static int PERMISSION_REQUEST_CODE = 200;
        string permission = Manifest.Permission.Camera;

        private bool IsNotification = false;
        private object NotificationData;
        internal static Context ActivityContext { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {

            //ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;
            ActivityContext = this;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
            RequestedOrientation = ScreenOrientation.Portrait;

            //UserDialogs.Init(this);

            FirebaseApp.InitializeApp(this);

            FirebasePushNotificationManager.ProcessIntent(this, Intent);

            /* //Set the default notification channel for your app when running Android Oreo
             if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
             {
                 //Change for your default notification channel id here
                 FirebasePushNotificationManager.DefaultNotificationChannelId = "FirebasePushNotificationChannel";

                 //Change for your default notification channel name here
                 FirebasePushNotificationManager.DefaultNotificationChannelName = "General";
             }

             FirebasePushNotificationManager.NotificationActivityType = typeof(MainActivity);
             //If debug you should reset the token each time.
 #if DEBUG
             FirebasePushNotificationManager.Initialize(this, false);
 #else
               FirebasePushNotificationManager.Initialize(this,false);
 #endif
            */
            /*//Handle notification when app is closed here - ELES DIZEM ISTO MAS EU SÒ APANHO COISAS AQUI QD A APP ESTÁ ABERTA
            CrossFirebasePushNotification.Current.OnNotificationReceived += (s, p) =>
            {
                Log.Debug("HUGO", "OnNotificationReceived Activity");

            };


            //Handle notification when app is closed here - ELES DIZEM ISTO MAS EU SÒ APANHO COISAS AQUI QD A APP ESTÁ ABERTA
            CrossFirebasePushNotification.Current.OnNotificationOpened += (s, p) =>
            {
                Log.Debug("HUGO", "OnNotificationOpened Activity");

            };

            FirebasePushNotificationManager.ProcessIntent(this, Intent);*/

            ServicePointManager.ServerCertificateValidationCallback += (o, cert, chain, errors) => true;

            CrossCurrentActivity.Current.Init(Application);



            if (ActivityCompat.CheckSelfPermission(this, permission) == Permission.Granted)
            {

            }
            else
            {
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.Camera, Manifest.Permission.WriteExternalStorage, Manifest.Permission.ReadExternalStorage }, PERMISSION_REQUEST_CODE);


            }

        }

        protected override void OnNewIntent(Intent intent)
        {
           base.OnNewIntent(intent);
           FirebasePushNotificationManager.ProcessIntent(this,intent);
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);

            DeviceOrientationImplementation.NotifyOrientationChange(newConfig.Orientation);
        }

        public event EventHandler<ActivityResultEventArgs> ActivityResult;

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);



        }
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            //Since we set the request code to 1 for both the camera and photo gallery, that's what we need to check for
            if (requestCode == 0)
            {
                if (resultCode == Result.Ok)
                {
                    ActivityResult?.Invoke(this, new ActivityResultEventArgs { Intent = data });
                    Task.Run(() =>
                    {
                        if (App.ImageIdToSave != null)
                        {
                            var documentsDirectry = ActivityContext.GetExternalFilesDir(Android.OS.Environment.DirectoryPictures);
                            string pngFilename = System.IO.Path.Combine(documentsDirectry.AbsolutePath, App.ImageIdToSave + "." + FileFormatEnum.JPEG.ToString());

                            if (File.Exists(pngFilename))
                            {
                                Java.IO.File file = new Java.IO.File(documentsDirectry, App.ImageIdToSave + "." + FileFormatEnum.JPEG.ToString());
                                Android.Net.Uri uri = Android.Net.Uri.FromFile(file);

                                //Read the meta data of the image to determine what orientation the image should be in
                                var originalMetadata = new ExifInterface(pngFilename);
                                int orientation = GetRotation(originalMetadata);

                                var fileName = App.ImageIdToSave + "." + FileFormatEnum.JPEG.ToString();
                                HandleBitmap(uri, orientation, fileName);
                            }
                        }
                    });
                }
            }
            else if (requestCode == 1)
            {
                if (resultCode == Result.Ok)
                {
                    if (data.Data != null)
                    {
                        //Grab the Uri which is holding the path to the image
                        Android.Net.Uri uri = data.Data;

                        string fileName = null;

                        if (App.ImageIdToSave != null)
                        {
                            fileName = App.ImageIdToSave + "." + FileFormatEnum.JPEG.ToString();
                            var pathToImage = GetPathToImage(uri);
                            var originalMetadata = new ExifInterface(pathToImage);
                            int orientation = GetRotation(originalMetadata);

                            HandleBitmap(uri, orientation, fileName);
                        }
                    }
                }
            }
        }

        private string GetPathToImage(Android.Net.Uri uri)
        {
            string doc_id = "";
            using (var c1 = ContentResolver.Query(uri, null, null, null, null))
            {
                c1.MoveToFirst();
                String document_id = c1.GetString(0);
                doc_id = document_id.Substring(document_id.LastIndexOf(":") + 1);
            }

            string path = null;

            // The projection contains the columns we want to return in our query.
            string selection = Android.Provider.MediaStore.Images.Media.InterfaceConsts.Id + " =? ";
            using (var cursor = ContentResolver.Query(Android.Provider.MediaStore.Images.Media.ExternalContentUri, null, selection, new string[] { doc_id }, null))
            {
                if (cursor == null) return path;
                var columnIndex = cursor.GetColumnIndexOrThrow(Android.Provider.MediaStore.Images.Media.InterfaceConsts.Data);
                cursor.MoveToFirst();
                path = cursor.GetString(columnIndex);
            }
            return path;
        }

        public int GetRotation(ExifInterface exif)
        {
            try
            {
                var orientation = (Android.Media.Orientation)exif.GetAttributeInt(ExifInterface.TagOrientation, (int)Android.Media.Orientation.Normal);

                switch (orientation)
                {
                    case Android.Media.Orientation.Rotate90:
                        return 90;
                    case Android.Media.Orientation.Rotate180:
                        return 180;
                    case Android.Media.Orientation.Rotate270:
                        return 270;
                    default:
                        return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 0;
            }
        }

        public void HandleBitmap(Android.Net.Uri uri, int orientation, string imageId)
        {
            try
            {
                Bitmap mBitmap;// = Android.Provider.MediaStore.Images.Media.GetBitmap(this.ContentResolver, uri);

                if (((int)Build.VERSION.SdkInt) < 29)
                {
                    mBitmap = Android.Provider.MediaStore.Images.Media.GetBitmap(this.ContentResolver, uri);
                }
                else
                {
                    ImageDecoder.Source source = ImageDecoder.CreateSource(this.ContentResolver, uri);
                    mBitmap = ImageDecoder.DecodeBitmap(source);
                }

                Android.Graphics.Bitmap myBitmap = null;

                if (mBitmap != null)
                {
                    //In order to rotate the image we create a Matrix object, rotate if the image is not already in it's correct orientation
                    Matrix matrix = new Matrix();
                    if (orientation != 0)
                    {
                        matrix.PreRotate(orientation);
                    }

                    Console.WriteLine("About to rotate");
                    myBitmap = Bitmap.CreateBitmap(mBitmap, 0, 0, mBitmap.Width, mBitmap.Height, matrix, true);

                    MemoryStream stream = new MemoryStream();

                    Console.WriteLine("About to compress");
                    //Compressing by 50%, feel free to change if file size is not a factor
                    myBitmap.Compress(Bitmap.CompressFormat.Jpeg, 50, stream);

                    Console.WriteLine("About to convert to byte array");
                    byte[] bitmapData = stream.ToArray();

                    //Send image byte array back to UI
                    Console.WriteLine("About to send Image back to UI");

                    if (imageId != null && imageId != "")
                    {
                        SavePictureToDisk(myBitmap, imageId);
                    }

                    MessagingCenter.Send<byte[]>(bitmapData, "ImageSelected");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void SavePictureToDisk(Bitmap source, string imageName)
        {
            try
            {
                Task.Run(() =>
                {
                    var documentsDirectry = ActivityContext.GetExternalFilesDir(Android.OS.Environment.DirectoryPictures);
                    string pngFilename = System.IO.Path.Combine(documentsDirectry.AbsolutePath, imageName);

                    //If the image already exists, delete, and make way for the updated one
                    if (File.Exists(pngFilename))
                    {
                        File.Delete(pngFilename);
                    }

                    using (FileStream fs = new FileStream(pngFilename, FileMode.OpenOrCreate))
                    {
                        source.Compress(Bitmap.CompressFormat.Jpeg, 50, fs);
                        fs.Close();
                    }

                    Console.WriteLine("Saved photo");
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public class ActivityResultEventArgs : EventArgs
        {

            public Intent Intent
            {
                get;
                set;
            }
        }
    }

}