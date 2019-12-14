using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
///Algorithms Project
///Intelligent Scissors
///

namespace ImageQuantization
{
    /// <summary>
    /// Holds the pixel color in 3 byte values: red, green and blue
    /// </summary>
    public struct RGBPixel
    {
        public byte red, green, blue;
    }
    public struct RGBPixelD
    {
        public double red, green, blue;
    }


    /// <summary>
    /// Library of static functions that deal with images
    /// </summary>
    public class ImageOperations
    {
        /// <summary>
        /// Open an image and load it into 2D array of colors (size: Height x Width)
        /// </summary>
        /// <param name="ImagePath">Image file path</param>
        /// <returns>2D array of colors</returns>
        public static RGBPixel[,] OpenImage(string ImagePath)
        {
            Bitmap original_bm = new Bitmap(ImagePath);
            int Height = original_bm.Height;
            int Width = original_bm.Width;

            RGBPixel[,] Buffer = new RGBPixel[Height, Width];

            unsafe
            {
                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
                int x, y;
                int nWidth = 0;
                bool Format32 = false;
                bool Format24 = false;
                bool Format8 = false;

                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    Format24 = true;
                    nWidth = Width * 3;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    Format32 = true;
                    nWidth = Width * 4;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Format8 = true;
                    nWidth = Width;
                }
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (Format8)
                        {
                            Buffer[y, x].red = Buffer[y, x].green = Buffer[y, x].blue = p[0];
                            p++;
                        }
                        else
                        {
                            Buffer[y, x].red = p[2];
                            Buffer[y, x].green = p[1];
                            Buffer[y, x].blue = p[0];
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }
            primMST(Buffer);
            return Buffer;
        }

        /// <summary>
        /// Get the height of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Height</returns>
        public static int GetHeight(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }

        /// <summary>
        /// Get the width of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Width</returns>
        public static int GetWidth(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }

        /// <summary>
        /// Display the given image on the given PictureBox object
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <param name="PicBox">PictureBox object to display the image on it</param>
        public static void DisplayImage(RGBPixel[,] ImageMatrix, PictureBox PicBox)
        {
            // Create Image:
            //==============
            int Height = ImageMatrix.GetLength(0);
            int Width = ImageMatrix.GetLength(1);

            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            unsafe
            {
                BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                int nWidth = 0;
                nWidth = Width * 3;
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {
                        p[2] = ImageMatrix[i, j].red;
                        p[1] = ImageMatrix[i, j].green;
                        p[0] = ImageMatrix[i, j].blue;
                        p += 3;
                    }

                    p += nOffset;
                }
                ImageBMP.UnlockBits(bmd);
            }
            PicBox.Image = ImageBMP;
        }


        /// <summary>
        /// Apply Gaussian smoothing filter to enhance the edge detection 
        /// </summary>
        /// <param name="ImageMatrix">Colored image matrix</param>
        /// <param name="filterSize">Gaussian mask size</param>
        /// <param name="sigma">Gaussian sigma</param>
        /// <returns>smoothed color image</returns>
        public static RGBPixel[,] GaussianFilter1D(RGBPixel[,] ImageMatrix, int filterSize, double sigma)
        {
            int Height = GetHeight(ImageMatrix);
            int Width = GetWidth(ImageMatrix);

            RGBPixelD[,] VerFiltered = new RGBPixelD[Height, Width];
            RGBPixel[,] Filtered = new RGBPixel[Height, Width];


            // Create Filter in Spatial Domain:
            //=================================
            //make the filter ODD size
            if (filterSize % 2 == 0) filterSize++;

            double[] Filter = new double[filterSize];

            //Compute Filter in Spatial Domain :
            //==================================
            double Sum1 = 0;
            int HalfSize = filterSize / 2;
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                //Filter[y+HalfSize] = (1.0 / (Math.Sqrt(2 * 22.0/7.0) * Segma)) * Math.Exp(-(double)(y*y) / (double)(2 * Segma * Segma)) ;
                Filter[y + HalfSize] = Math.Exp(-(double)(y * y) / (double)(2 * sigma * sigma));
                Sum1 += Filter[y + HalfSize];
            }
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                Filter[y + HalfSize] /= Sum1;
            }

            //Filter Original Image Vertically:
            //=================================
            int ii, jj;
            RGBPixelD Sum;
            RGBPixel Item1;
            RGBPixelD Item2;

            for (int j = 0; j < Width; j++)
                for (int i = 0; i < Height; i++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int y = -HalfSize; y <= HalfSize; y++)
                    {
                        ii = i + y;
                        if (ii >= 0 && ii < Height)
                        {
                            Item1 = ImageMatrix[ii, j];
                            Sum.red += Filter[y + HalfSize] * Item1.red;
                            Sum.green += Filter[y + HalfSize] * Item1.green;
                            Sum.blue += Filter[y + HalfSize] * Item1.blue;
                        }
                    }
                    VerFiltered[i, j] = Sum;
                }

            //Filter Resulting Image Horizontally:
            //===================================
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int x = -HalfSize; x <= HalfSize; x++)
                    {
                        jj = j + x;
                        if (jj >= 0 && jj < Width)
                        {
                            Item2 = VerFiltered[i, jj];
                            Sum.red += Filter[x + HalfSize] * Item2.red;
                            Sum.green += Filter[x + HalfSize] * Item2.green;
                            Sum.blue += Filter[x + HalfSize] * Item2.blue;
                        }
                    }
                    Filtered[i, j].red = (byte)Sum.red;
                    Filtered[i, j].green = (byte)Sum.green;
                    Filtered[i, j].blue = (byte)Sum.blue;
                }

            return Filtered;
        }



        public static RGBPixel[] constructGraph(RGBPixel[,] imageMatrix)
        {
            int height = GetHeight(imageMatrix), width = GetWidth(imageMatrix);
            Dictionary<RGBPixel, int> distinctColors = new Dictionary<RGBPixel, int>();
            int counter = 0;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    if (!(distinctColors.ContainsKey(imageMatrix[i, j])))
                    {
                        distinctColors[imageMatrix[i, j]] = counter;
                        counter++;
                    }

                }
            }

            Console.WriteLine("The Output is:");
            Console.Write("Number of Distinct colors: ");
            Console.WriteLine(distinctColors.Count);
            int s = distinctColors.Count;
            RGBPixel[] Graph = new RGBPixel[s];
            foreach (var it in distinctColors)
            {
                Graph[it.Value] = it.Key;

            }
            return Graph;

        }
        public static double GetEgdeWeight(RGBPixel Color1, RGBPixel Color2)
        {
            return Math.Sqrt((Math.Pow(Color1.blue - Color2.blue, 2)) + (Math.Pow(Color1.red - Color2.red, 2)) + (Math.Pow(Color1.green - Color2.green, 2)));
        }
        public class HeapNode
        {
            public int vertex;
            public double key;
            public HeapNode()
            {
            }
        }

        public class ResultSet
        {
            public int parent;
            public double weight;
            public ResultSet()
            {
            }
        }


        public class MinHeap
        {

            public int capacity;
            public int Size;
            public HeapNode[] mH;
            public int[] indexes;


            public MinHeap(RGBPixel[] G)
            {
                this.capacity = G.Length;
                mH = new HeapNode[capacity + 1];
                mH[0] = new HeapNode();
                mH[0].key = double.MinValue;
                indexes = new int[G.Length];
                //mH[0].vertex = null;
                Size = 0;
            }

            public void insert(HeapNode x)
            {
                Size++;
                int idx = Size;
                mH[idx] = x;
                indexes[x.vertex] = idx;
                bubbleUp(idx);
            }

            public void bubbleUp(int pos)
            {
                int parentIdx = pos / 2;
                int currentIdx = pos;
                while (currentIdx > 0 && mH[parentIdx].key > mH[currentIdx].key)
                {
                    HeapNode currentNode = mH[currentIdx];
                    HeapNode parentNode = mH[parentIdx];

                    indexes[currentNode.vertex] = parentIdx;
                    indexes[parentNode.vertex] = currentIdx;
                    swap(currentIdx, parentIdx);
                    currentIdx = parentIdx;
                    parentIdx = parentIdx / 2;
                }
            }

            public HeapNode extractMin()
            {
                HeapNode min = mH[1];
                HeapNode lastNode = mH[Size];
                indexes[lastNode.vertex] = 1;
                mH[1] = lastNode;
                mH[Size] = null;
                sinkDown(1);
                Size--;
                return min;
            }

            public void sinkDown(int k)
            {
                int smallest = k;
                int leftChildIdx = 2 * k;
                int rightChildIdx = 2 * k + 1;
                if (leftChildIdx < heapSize() && mH[smallest].key > mH[leftChildIdx].key)
                {
                    smallest = leftChildIdx;
                }
                if (rightChildIdx < heapSize() && mH[smallest].key > mH[rightChildIdx].key)
                {
                    smallest = rightChildIdx;
                }
                if (smallest != k)
                {

                    HeapNode smallestNode = mH[smallest];
                    HeapNode kNode = mH[k];

                    //swap the positions
                    indexes[smallestNode.vertex] = k;
                    indexes[kNode.vertex] = smallest;
                    swap(k, smallest);
                    sinkDown(smallest);
                }
            }

            public void swap(int a, int b)
            {
                HeapNode temp = mH[a];
                mH[a] = mH[b];
                mH[b] = temp;
            }

            public bool isEmpty()
            {
                return Size == 0;
            }

            public int heapSize()
            {
                return Size;
            }
        }

        public static void primMST(RGBPixel[,] imagematrix)
        {
            RGBPixel[] Graph = constructGraph(imagematrix);
            int size = Graph.Length;
            bool[] inHeap = new bool[size];
            ResultSet[] resultSet = new ResultSet[size];
            double[] key = new double[size];
            HeapNode[] heapNodes = new HeapNode[size];

            for (int i = 0; i < size; i++)
            {
                heapNodes[i] = new HeapNode();
                heapNodes[i].vertex = i;
                heapNodes[i].key = double.MaxValue;
                resultSet[i] = new ResultSet();
                inHeap[i] = true;
                key[i] = double.MaxValue;
            }
            heapNodes[0].key = 0;

            MinHeap minHeap = new MinHeap(Graph);
            for (int i = 0; i < size; i++)
            {
                minHeap.insert(heapNodes[i]);
            }

            while (!minHeap.isEmpty())
            {
                HeapNode extractedNode = minHeap.extractMin();

                int extractedVertex = extractedNode.vertex;
                inHeap[extractedVertex] = false;

                for (int i = 0; i < Graph.Length; i++)
                {
                    if (inHeap[i])
                    {
                        double newKey = GetEgdeWeight(Graph[extractedVertex], Graph[i]);
                        if (key[i] > newKey)
                        {

                            decreaseKey(minHeap, newKey, i);
                            resultSet[i].parent = extractedVertex;
                            resultSet[i].weight = newKey;
                            key[i] = newKey;
                        }
                    }
                }
            }

            double W = printMST(resultSet, Graph);
        }

        public static void decreaseKey(MinHeap minHeap, double newKey, int vertex)
        {

            int index = minHeap.indexes[vertex];

            HeapNode node = minHeap.mH[index];
            node.key = newKey;
            minHeap.bubbleUp(index);
        }

        public static double printMST(ResultSet[] resultSet, RGBPixel[] graph)
        {
            double total_min_weight = 0;

            for (int i = 0; i < graph.Length; i++)
            {
                total_min_weight += resultSet[i].weight;
            }
            Console.Write("Sum of MST is: ");
            Console.WriteLine(total_min_weight);
            return total_min_weight;
        }
    }
}

