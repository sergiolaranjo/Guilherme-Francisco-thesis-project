#if UNITY_WSA
using Dicom;
using Dicom.Imaging;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class DicomUtils
{


    public string savePath = @"Assets/Resources/";
	public string filename = "heart"; 
    private Texture3D texture3D;
    private int[] imageShape;
    public DicomUtils(string dicomFolder) {
        GetTexture3D(dicomFolder);
     }

    private void GetTexture3D(string dicomFolder) {
        Texture3D texture = Resources.Load<Texture3D>(filename);

        Debug.Log("Texture size:" + "(" + texture.width + texture.depth + texture.height + ")" );
        
        if (texture != null)
        {
            texture3D = texture;
            return;
        } else {
            #if UNITY_WSA
            CreateTexture(dicomFolder);
            AssetDatabase.CreateAsset (texture3D, savePath + filename + ".asset");
            #endif
        }
    }

#if UNITY_WSA
    public void CreateTexture(string dicomFolder) {
        List<DicomFile> dicomArray = ImportDicomImages(dicomFolder);

        int rows = dicomArray[0].Dataset.Get<int>(DicomTag.Rows);
        int columns = dicomArray[0].Dataset.Get<int>(DicomTag.Columns);
        int numSlices = dicomArray.Count;

        Debug.Log("Number of Rows: " + rows);
        Debug.Log("Number of Columns: " + columns);
        Debug.Log("Number of Slices: " + numSlices);

        imageShape = new int[] { rows, columns, numSlices };

        UnityEngine.Color32[] voxelColors = new UnityEngine.Color32[rows * columns * numSlices];

        for (int i = 0; i < numSlices; i++)
        {
            var image = new DicomImage(dicomArray[i].Dataset);
            var texture = image.RenderImage().AsTexture2D();

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    int index = col + row * columns + i * columns * rows;
                    voxelColors[index] = texture.GetPixel(row, col);
                }
            }
        }

        texture3D = new Texture3D(rows, columns, numSlices, TextureFormat.RGBA32, false);
        texture3D.SetPixels32(voxelColors);
        texture3D.Apply();
    }

    private List<DicomFile> ImportDicomImages(string dicomFolder)
    {
        List<DicomFile> files = new();

        foreach (string fileName in Directory.EnumerateFiles(dicomFolder))
        {
            if (Path.GetExtension(fileName).ToLower() == ".dcm")
            {
                files.Add(DicomFile.Open(fileName));
            }
        }

        List<DicomFile> slices = new();
        int skipCount = 0;

        foreach (DicomFile file in files)
        {
            if (file.Dataset.Contains(DicomTag.SliceLocation))
            {
                slices.Add(file);
            }
            else
            {
                skipCount++;
            }
        }

        Debug.Log($"Skipped, no SliceLocation: {skipCount}");

        return slices.OrderBy(s => s.Dataset.Get<double>(DicomTag.SliceLocation)).ToList();
    }
#endif
    public Sprite GetSpriteFromTexture(Texture2D texture) {
        Vector2 pivot = new(0.5f, 0.5f);
        Vector4 border = Vector4.zero; 
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, 1f, 0, SpriteMeshType.FullRect, border);
    }

    public Texture2D GetSagittalSliceTexture(int sliceIndex)
    {
        int height = texture3D.height;
        int depth = texture3D.depth;

        UnityEngine.Color32[] pixels = new UnityEngine.Color32[height * depth];

        for (int x = 0; x < height; x++){
            for(int z = 0; z < depth; z++) {
                pixels[x * depth + z] = texture3D.GetPixel(x, sliceIndex, z);
            }
        }

        Texture2D sliceTexture = new(depth, height, TextureFormat.RGBA32, false);
        sliceTexture.SetPixels32(pixels);
        sliceTexture.Apply();

        return sliceTexture;
    }


    public Texture2D GetCoronalSliceTexture(int sliceIndex)
    {
        int width = texture3D.width;
        int depth = texture3D.depth;

        UnityEngine.Color32[] pixels = new UnityEngine.Color32[width * depth];

        
        for (int z = 0; z < depth; z++)
        {
            for (int y = 0; y < width; y++)
            {
                pixels[z * width + y] = texture3D.GetPixel(sliceIndex, y, depth - z - 1);
            }
        }


        Texture2D sliceTexture = new Texture2D(width, depth, TextureFormat.RGBA32, false);
        sliceTexture.SetPixels32(pixels);
        sliceTexture.Apply();

        return sliceTexture;
    }

    public Texture2D GetAxialSliceTexture(int sliceIndex)
    {
        int width = texture3D.width;
        int height = texture3D.height;

        UnityEngine.Color32[] pixels =  new UnityEngine.Color32[width * height];


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y< height; y++)
            {
                pixels[x * height + y] = texture3D.GetPixel(x, y, sliceIndex);
            }
        }

        Texture2D sliceTexture = new(width, height, TextureFormat.RGBA32, false);
        sliceTexture.SetPixels32(pixels);
        sliceTexture.Apply();

        return sliceTexture;
    }

    public int[] GetImageShape() {
        imageShape = new int[] { texture3D.width, texture3D.height, texture3D.depth };
        return imageShape;
    }
}
