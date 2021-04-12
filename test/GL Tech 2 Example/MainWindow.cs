﻿using Game.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GLTech2;

namespace Game
{
    public partial class MainWindow : Form
    {
        Camera myCamera;
        Map myMap;
        

        public MainWindow() =>
            InitializeComponent();


        public void OnLoad(object sender, EventArgs e)
        {
            //Allocate space in memory to the map and create a camera.
            myMap = new Map(maxWalls: 512, maxSprities: 512);

            //Texture32 is only alocated once for every object that refers to it.
            Texture32 wallTexture = new Texture32(Resources.Wall);
            Texture32 cosmosTexture = new Texture32(Resources.Universe);

            //Material is a flyweight structure that refers to a texture and is alocated one for each object that uses.
            Material wallMaterial = new Material(
                texture: wallTexture,
                hoffset: 0f,
                hrepeat: 12f);

            Material cosmosMaterial = new Material(
                texture: cosmosTexture,
                hoffset: 0f,
                hrepeat: 3f);

            //Create a cilinder and add it to the map.
            Vector[] verts = Vector.GetPolygon(
                center: new Vector(3f, 2f),
                radius: 4f,
                edges: 400);

            Wall[] myWalls = Wall.CreatePolygon(wallMaterial, verts);

            myMap.AddWalls(myWalls);

            //Create your camera.
            myCamera = new Camera(
                map: myMap,
                background: cosmosMaterial,
                output: display,
                width: 1600,
                height: 900);

            //Subscribe to camera.OnRender event your custom Update method wich will be called whenever the camera renders a new frame.
            myCamera.OnRender += (a, aa) => Update(a, aa);

            //Start a continuous rendering process.
            myCamera.StartRendering();
        }

        private void RefreshPictureBox(object sender, PaintEventArgs e)
        {
            (sender as PictureBox).Image = myCamera.BitmapCopy;
        }

        //Do whatever you want each time the engine generates a new frame.
        double[] timeRegistry = new double[1000];
        int registryCount = 0;
        public void Update(Camera sender, double deltaTime)
        {
            timeRegistry[registryCount++] = deltaTime * 1000;
            if (registryCount == 1000)
            {
                Console.WriteLine("Average: " + timeRegistry.Average());
                Console.WriteLine("SD: " + StdDeviation(timeRegistry));
                registryCount = 0;
            }

            sender.Step(1f * (float) deltaTime);
            sender.CameraAngle += 25f * (float) deltaTime;
        }

        private double StdDeviation(IEnumerable<double> values)
        {
            double result = 0;

            if (values.Any())
            {
                double avg = values.Average();    
                double sum = values.Sum(d => Math.Pow(d - avg, 2));  
                result = Math.Sqrt((sum) / (values.Count() - 1));
            }

            return result;
        }
    }
}
