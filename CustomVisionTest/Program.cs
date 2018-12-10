using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;

namespace CustomVisionTest
{
    class Program
    {
        private static List<MemoryStream> perrosImages;
        private static MemoryStream testImage;

        static void Main(string[] args)
        {
            // Agrega las claves que obtuviste en la pantalla de configuración del portal customvision.ai 
            string trainingKey = "72364176e08b421b872494d9ba45e10b";
            string predictionKey = "4380e058860d4ceabfa432fd75e740bf";
            string rutaImagenes = @"D:\Images\Train";
            string ImagenAReconocer = @"D:\Images\Test\ImageToTest.jpg";

            // Creamos el API para utilizar los servicios de Custom Vision, dando nuestra Key
            TrainingApi trainingApi = new TrainingApi() { ApiKey = trainingKey };

            // Creamos el proyecto, y lo nombraremos como 'Reconocimiento'. Ten en cuenta que aparecerá
            // en el portal de Custom Vision con este nombre, por lo que puedes cambiarlo según consideres 
            Console.WriteLine("Creando proyecto...");
            var project = trainingApi.CreateProject("Reconocimiento"); // el proyecto puedes llamarlo como quieras

            // Creamos un tag 'Perros' para identificar imágenes de perros. Ten en cuenta que aparecerá
            // en el portal de Custom Vision bajo este Tag, por lo que puedes cambiarlo según las imágenes que utilices 
            Console.WriteLine("Creando tag...");
            var perrosTag = trainingApi.CreateTag(project.Id, "Perros");

            // Cargamos las imágenes en memoria 
            Console.WriteLine("Subiendo imágenes...");
            perrosImages = Directory.GetFiles(rutaImagenes).Select(f => new MemoryStream(File.ReadAllBytes(f))).ToList();

            // subimos las imágenes al portal  
            foreach (var image in perrosImages)
            {
                trainingApi.CreateImagesFromData(project.Id, image, new List<string>() { perrosTag.Id.ToString() });
            }

            // Lanzamos el entrenamiento de las imágenes con sus tags asociados   
            Console.WriteLine("Entrenando...");
            var iteraccion = trainingApi.TrainProject(project.Id);

            // Mientras el status del entrenamiento esté en curso esperamos medio segundo y volvemos a preguntar
            while (iteraccion.Status == "Training")
            {
                Thread.Sleep(500);

                // y volvemos a actualizar el status  
                iteraccion = trainingApi.GetIteration(project.Id, iteraccion.Id);
            }

            // Entrenamiento realizado. Marcamos este entrenamiento como endpoint por defecto
            iteraccion.IsDefault = true;
            trainingApi.UpdateIteration(project.Id, iteraccion.Id, iteraccion);
            Console.WriteLine("Entrenamiento finalizado...");

            // tenemos una iteracción entrenada. vamos a probarla, cargamos imagen
            Console.WriteLine("Cargando imagen para predicción...");
            testImage = new MemoryStream(File.ReadAllBytes(ImagenAReconocer));

            // preparamos el endpoint de reconocimiento
            PredictionEndpoint endpoint = new PredictionEndpoint() { ApiKey = predictionKey };

            // realizamos la predicción  
            Console.WriteLine("Ejecutando predicción...");
            var result = endpoint.PredictImage(project.Id, testImage);

            // mostramos los resultados para cada tag que tengamos  
            foreach (var c in result.Predictions)
            {
                Console.WriteLine($"\t{c.TagName}: {c.Probability:P1}");
            }
            Console.ReadKey();
                        
        }
    }
}
