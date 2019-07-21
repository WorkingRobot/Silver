using ModelViewer.Properties;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using OpenTkControl;
using PakReader;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace Silver.ModelViewer
{
    public class ModelInterface
    {
        int ibo_elements;
        ConcurrentDictionary<int, Volume> objects = new ConcurrentDictionary<int, Volume>();
        Skybox Skybox;
        Text FrameCounter;

        Camera cam = new Camera();
        //List<Light> lights = new List<Light>(MAX_LIGHTS);
        //const int MAX_LIGHTS = 5;

        Matrix4 view = Matrix4.Identity;
        Matrix4 projectionMatrix = Matrix4.Identity;
        Matrix4 screenMatrix = Matrix4.Identity;

        public ConcurrentDictionary<string, int> textures = new ConcurrentDictionary<string, int>();

        ConcurrentDictionary<string, ShaderProgram> shaders = new ConcurrentDictionary<string, ShaderProgram>();
        ConcurrentDictionary<string, Material> materials = new ConcurrentDictionary<string, Material>();

        double time = 0.0f;
        double prevT = Environment.TickCount / 1000d;
        double timeDelta;

        int frameC = 0;
        double frameTime = 0;

        int w, h;
        bool loaded;

        bool forceUnfocus;

        USkeletalMesh mesh;
        Func<string, PakPackage> packageFunc;

        public ModelInterface(USkeletalMesh mesh, int w, int h, Func<string, PakPackage> packageFunc)
        {
            this.w = w;
            this.h = h;
            this.mesh = mesh;
            this.packageFunc = packageFunc;
        }

        bool Errored;
        void InitProgram()
        {
            try
            {
                cam.MouseSensitivity = 0.0025f;
                Natives.SetCursorPos(64 / 2, 64 / 2);

                GL.GenBuffers(1, out ibo_elements);

                LoadResources();

                SetupScene();

                GL.ClearColor(Color.CornflowerBlue);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Errored = true;
            }
        }

        private void LoadResources()
        {
            //shaders.TryAdd("default", new ShaderProgram(Resources.vs, Resources.fs));
            shaders.TryAdd("textured", new ShaderProgram(Resources.vs_tex, Resources.fs_tex));
            //shaders.TryAdd("normal", new ShaderProgram("vs_norm.glsl", "fs_norm.glsl", true));
            //shaders.TryAdd("lit", new ShaderProgram("vs_lit.glsl", "fs_lit.glsl", true));
            //shaders.TryAdd("lit_multiple", new ShaderProgram("vs_lit.glsl", "fs_lit_multiple.glsl", true));
            //shaders.TryAdd("lit_advanced", new ShaderProgram("vs_lit.glsl", "fs_lit_advanced.glsl", true));

            // Load materials and textures
            LoadMaterials(Resources.skyboxmtl);
        }

        private void SetupScene()
        {
            Skybox = new Skybox
            {
                TextureID = materials["skybox"].DiffuseMap,
                Scale = new Vector3(500, 500, 500),
                Material = materials["skybox"],
                Enabled = false
            };
            AddObject(Skybox);
            AddObject(FrameCounter = new Text((k, v) => textures[k] = v));

            var cSkel = new CSkeletalMesh(mesh);
            for (int i = 0; i < cSkel.Lods[0].Sections.Length; i++)
            {
                AddObject(new UMeshSection(cSkel, mesh, i, new int[0], this, packageFunc)
                {
                    Scale = new Vector3(.5f),
                    Rotation = new Vector3(-(float)Math.PI / 2, (float)Math.PI, 0)
                });
            }

            /*/ Create lights
            lights.Add(new Light(Vector3.Zero, new Vector3(.65f))
            {
                Type = LightType.Directional,
                Direction = new Vector3(-2, 5, 5).Normalized()
            });

            lights.Add(new Light(Vector3.Zero, new Vector3(.65f))
            {
                Type = LightType.Directional,
                Direction = new Vector3(.5f, 5, -5).Normalized()
            });
            */

            // Move camera away from origin
            cam.Position = new Vector3(0f, 1f, 3f);
        }

        public void OnRender(OpenTkControlBase.GlRenderEventArgs e)
        {
            if (Errored) return;
            try
            {
                if (!loaded)
                {
                    InitProgram();
                    loaded = true;
                }
                if (e.Resized)
                {
                    w = e.Width;
                    h = e.Height;
                }
                UpdateFrame();

                GL.Viewport(0, 0, w, h);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.Enable(EnableCap.DepthTest);
                foreach (var shader in shaders)
                {
                    RenderShader(shader.Key, shader.Value);
                }
                GL.Flush();
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
            }
        }

        private void UpdateFrame()
        {
            ProcessInput();
            
            double tPrev = Environment.TickCount / 1000d;
            timeDelta = tPrev - prevT;
            time += timeDelta;
            prevT = tPrev;
            frameC++;
            if (frameTime == 0 || tPrev - frameTime > .25)
            {
                FrameCounter.SetText($"{frameC * 4} FPS");
                frameC = 0;
                frameTime = tPrev;
            }

            if (Skybox.Enabled) Skybox.Position = cam.Position;

            view = cam.GetViewMatrix();
            projectionMatrix = cam.GetViewProjectionMatrix(w / (float)h);
            screenMatrix = cam.GetScreenProjectionMatrix(w / (float)h);
            // FrustumCullingHelper.ExtractFrustum(projectionMatrix); unused, it's just one mesh right now

            // move to top left corner if a resize ever somehow happens
            FrameCounter.Position = new Vector3(-50 * (w / (float)h), 50, 0);

            LoadImageTask();
        }

        private void RenderShader(string shader, ShaderProgram prog)
        {
            int vertC = 0, indsC = 0, colorC = 0, texC = 0, normC = 0;

            List<Volume> volumes = new List<Volume>();
            foreach (var v in objects.Values)
            {
                if (v.Enabled && v.Shader == shader)
                {
                    vertC += v.VertCount;
                    indsC += v.IndiceCount;
                    colorC += v.ColorDataCount;
                    texC += v.TextureCoordsCount;
                    normC += v.NormalCount;
                    volumes.Add(v);
                }
            }
            if (volumes.Count == 0) return;

            Vector3[] verts = new Vector3[vertC];
            int[] inds = new int[indsC];
            Vector3[] colors = new Vector3[colorC];
            Vector2[] texcoords = new Vector2[texC];
            Vector3[] normals = new Vector3[normC];

            vertC = 0; indsC = 0; colorC = 0; texC = 0; normC = 0;
            for (int i = 0; i < volumes.Count; i++)
            {
                var v = volumes[i];
                Array.Copy(v.GetVerts(), 0, verts, vertC, v.VertCount);
                Array.Copy(v.GetIndices(vertC), 0, inds, indsC, v.IndiceCount);
                Array.Copy(v.GetColorData(), 0, colors, colorC, v.ColorDataCount);
                Array.Copy(v.GetTextureCoords(), 0, texcoords, texC, v.TextureCoordsCount);
                Array.Copy(v.GetNormals(), 0, normals, normC, v.NormalCount);
                vertC += v.VertCount;
                indsC += v.IndiceCount;
                colorC += v.ColorDataCount;
                texC += v.TextureCoordsCount;
                normC += v.NormalCount;
            }

            foreach (Volume v in volumes)
            {
                v.CalculateModelMatrix();

                if (v.WorldSpace)
                {
                    v.ModelViewProjectionMatrix = v.ModelMatrix * projectionMatrix;
                }
                else
                {
                    v.ModelViewProjectionMatrix = v.ModelMatrix * screenMatrix;
                }
            }

            GL.UseProgram(prog.ProgramID);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // Buffer index data
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo_elements);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(inds.Length * sizeof(int)), inds, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, prog.GetBuffer("vPosition"));

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(verts.Length * Vector3.SizeInBytes), verts, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(prog.GetAttribute("vPosition"), 3, VertexAttribPointerType.Float, false, 0, 0);

            // Buffer vertex color if shader supports it
            if (prog.GetAttribute("vColor") != -1)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, prog.GetBuffer("vColor"));
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(colors.Length * Vector3.SizeInBytes), colors, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(prog.GetAttribute("vColor"), 3, VertexAttribPointerType.Float, true, 0, 0);
            }


            // Buffer texture coordinates if shader supports it
            if (prog.GetAttribute("texcoord") != -1)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, prog.GetBuffer("texcoord"));
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(texcoords.Length * Vector2.SizeInBytes), texcoords, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(prog.GetAttribute("texcoord"), 2, VertexAttribPointerType.Float, true, 0, 0);
            }

            if (prog.GetAttribute("vNormal") != -1)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, prog.GetBuffer("vNormal"));
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(normals.Length * Vector3.SizeInBytes), normals, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(prog.GetAttribute("vNormal"), 3, VertexAttribPointerType.Float, true, 0, 0);
            }
            prog.EnableVertexAttribArrays();

            int indiceat = 0;

            // Draw all objects
            foreach (Volume v in volumes)
            {
                if (!textures.TryGetValue(v.TextureID ?? "", out int texId))
                {
                    texId = textures.First().Value;
                }
                GL.BindTexture(TextureTarget.Texture2D, texId);

                GL.UniformMatrix4(prog.GetUniform("modelview"), false, ref v.ModelViewProjectionMatrix);

                if (prog.GetAttribute("maintexture") != -1)
                {
                    GL.Uniform1(prog.GetAttribute("maintexture"), texId);
                }

                if (prog.GetUniform("view") != -1)
                {
                    GL.UniformMatrix4(prog.GetUniform("view"), false, ref view);
                }

                if (prog.GetUniform("model") != -1)
                {
                    GL.UniformMatrix4(prog.GetUniform("model"), false, ref v.ModelMatrix);
                }

                if (prog.GetUniform("material_ambient") != -1)
                {
                    GL.Uniform3(prog.GetUniform("material_ambient"), ref v.Material.AmbientColor);
                }

                if (prog.GetUniform("material_diffuse") != -1)
                {
                    GL.Uniform3(prog.GetUniform("material_diffuse"), ref v.Material.DiffuseColor);
                }

                if (prog.GetUniform("material_specular") != -1)
                {
                    GL.Uniform3(prog.GetUniform("material_specular"), ref v.Material.SpecularColor);
                }

                if (prog.GetUniform("material_specExponent") != -1)
                {
                    GL.Uniform1(prog.GetUniform("material_specExponent"), v.Material.SpecularExponent);
                }

                if (prog.GetUniform("map_specular") != -1)
                {
                    // Object has a specular map
                    if (!string.IsNullOrEmpty(v.Material.SpecularMap))
                    {
                        GL.ActiveTexture(TextureUnit.Texture1);
                        GL.BindTexture(TextureTarget.Texture2D, textures[v.Material.SpecularMap]);
                        GL.Uniform1(prog.GetUniform("map_specular"), 1);
                        GL.Uniform1(prog.GetUniform("hasSpecularMap"), 1);
                        GL.ActiveTexture(TextureUnit.Texture0);
                    }
                    else // Object has no specular map
                    {
                        GL.Uniform1(prog.GetUniform("hasSpecularMap"), 0);
                    }
                }

                /*
                if (prog.GetUniform("light_position") != -1)
                {
                    GL.Uniform3(prog.GetUniform("light_position"), ref lights[0].Position);
                }

                if (prog.GetUniform("light_color") != -1)
                {
                    GL.Uniform3(prog.GetUniform("light_color"), ref lights[0].Color);
                }

                if (prog.GetUniform("light_diffuseIntensity") != -1)
                {
                    GL.Uniform1(prog.GetUniform("light_diffuseIntensity"), lights[0].DiffuseIntensity);
                }

                if (prog.GetUniform("light_ambientIntensity") != -1)
                {
                    GL.Uniform1(prog.GetUniform("light_ambientIntensity"), lights[0].AmbientIntensity);
                }


                for (int i = 0; i < Math.Min(lights.Count, MAX_LIGHTS); i++)
                {
                    if (prog.GetUniform("lights[" + i + "].position") != -1)
                    {
                        GL.Uniform3(prog.GetUniform("lights[" + i + "].position"), ref lights[i].Position);
                    }

                    if (prog.GetUniform("lights[" + i + "].color") != -1)
                    {
                        GL.Uniform3(prog.GetUniform("lights[" + i + "].color"), ref lights[i].Color);
                    }

                    if (prog.GetUniform("lights[" + i + "].diffuseIntensity") != -1)
                    {
                        GL.Uniform1(prog.GetUniform("lights[" + i + "].diffuseIntensity"), lights[i].DiffuseIntensity);
                    }

                    if (prog.GetUniform("lights[" + i + "].ambientIntensity") != -1)
                    {
                        GL.Uniform1(prog.GetUniform("lights[" + i + "].ambientIntensity"), lights[i].AmbientIntensity);
                    }

                    if (prog.GetUniform("lights[" + i + "].direction") != -1)
                    {
                        GL.Uniform3(prog.GetUniform("lights[" + i + "].direction"), ref lights[i].Direction);
                    }

                    if (prog.GetUniform("lights[" + i + "].type") != -1)
                    {
                        GL.Uniform1(prog.GetUniform("lights[" + i + "].type"), (int)lights[i].Type);
                    }

                    if (prog.GetUniform("lights[" + i + "].coneAngle") != -1)
                    {
                        GL.Uniform1(prog.GetUniform("lights[" + i + "].coneAngle"), lights[i].ConeAngle);
                    }

                    if (prog.GetUniform("lights[" + i + "].linearAttenuation") != -1)
                    {
                        GL.Uniform1(prog.GetUniform("lights[" + i + "].linearAttenuation"), lights[i].LinearAttenuation);
                    }

                    if (prog.GetUniform("lights[" + i + "].quadraticAttenuation") != -1)
                    {
                        GL.Uniform1(prog.GetUniform("lights[" + i + "].quadraticAttenuation"), lights[i].QuadraticAttenuation);
                    }
                }*/

                GL.DrawElements(BeginMode.Triangles, v.IndiceCount, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                indiceat += v.IndiceCount;
            }
            prog.DisableVertexAttribArrays();
        }

        bool escPressed = false;
        bool tabPressed = false;
        private void ProcessInput()
        {
            KeyboardState state = Keyboard.GetState();
            if (state.IsKeyDown(Key.Escape))
            {
                if (!escPressed)
                {
                    forceUnfocus = !forceUnfocus;
                    escPressed = true;
                }
            }
            else
            {
                escPressed = false;
            }

            if (forceUnfocus || !Natives.Focused()) return;

            if (state.IsKeyDown(Key.W))
            {
                cam.Move(0f, 0.1f, 0f);
            }
            if (state.IsKeyDown(Key.S))
            {
                cam.Move(0f, -0.1f, 0f);
            }
            if (state.IsKeyDown(Key.A))
            {
                cam.Move(-0.1f, 0f, 0f);
            }
            if (state.IsKeyDown(Key.D))
            {
                cam.Move(0.1f, 0f, 0f);
            }
            if (state.IsKeyDown(Key.Space))
            {
                cam.Move(0f, 0f, 0.1f);
            }
            if (state.IsKeyDown(Key.LShift))
            {
                cam.Move(0f, 0f, -0.1f);
            }
            if (state.IsKeyDown(Key.Tab))
            {
                if (!tabPressed)
                {
                    Skybox.Enabled = !Skybox.Enabled;
                    tabPressed = true;
                }
            }
            else
            {
                tabPressed = false;
            }

            Vector2 delta = Natives.GetMousePosition();
            cam.AddRotation(64 / 2 - delta.X, 64 / 2 - delta.Y);
        }

        int objC = 0; // Because ConcurrentList doesn't exist and ConcurrentBag makes the rendering glitchy af
        public void AddObject(Volume vol)
        {
            objects[objC++] = vol;
        }

        private void LoadMaterials(byte[] res)
        {
            foreach (var mat in Material.LoadFromResource(res))
            {
                if (!materials.ContainsKey(mat.Key))
                {
                    materials.TryAdd(mat.Key, mat.Value);
                    LoadMatTextures(mat.Value);
                }
            }
        }

        private void LoadMatTextures(Material mat)
        {
            if (File.Exists(mat.AmbientMap) && !textures.ContainsKey(mat.AmbientMap))
            {
                textures.TryAdd(mat.AmbientMap, LoadImage(mat.AmbientMap));
            }

            if (File.Exists(mat.DiffuseMap) && !textures.ContainsKey(mat.DiffuseMap))
            {
                textures.TryAdd(mat.DiffuseMap, LoadImage(mat.DiffuseMap));
            }

            if (File.Exists(mat.SpecularMap) && !textures.ContainsKey(mat.SpecularMap))
            {
                textures.TryAdd(mat.SpecularMap, LoadImage(mat.SpecularMap));
            }

            if (File.Exists(mat.NormalMap) && !textures.ContainsKey(mat.NormalMap))
            {
                textures.TryAdd(mat.NormalMap, LoadImage(mat.NormalMap));
            }

            if (File.Exists(mat.OpacityMap) && !textures.ContainsKey(mat.OpacityMap))
            {
                textures.TryAdd(mat.OpacityMap, LoadImage(mat.OpacityMap));
            }
        }

        public static int LoadImage(Bitmap image)
        {
            int texID = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, texID);

            using (image)
            {
                BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                    PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                image.UnlockBits(data);
            }

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            return texID;
        }

        ConcurrentDictionary<string, SKBitmap> loadTexQueue = new ConcurrentDictionary<string, SKBitmap>();
        public void ValidifyQueueLoad(string tex) => QueueLoad(tex, SKBitmap.FromImage(packageFunc(UMeshSection.ValidifyPath(tex)).GetTexture()));
        public void QueueLoad(string tex) => QueueLoad(tex, SKBitmap.FromImage(packageFunc(tex).GetTexture()));

        public void QueueLoad(string tex, SKBitmap image)
        {
            if (textures.ContainsKey(tex)) return;
            if (image.ColorType != SKColorType.Bgra8888)
            {
                SKBitmap img;
                using (image)
                {
                    img = image.Copy(SKColorType.Bgra8888);
                }
                image = img;
            }
            loadTexQueue[tex] = image;
        }

        public void LoadImageTask()
        {
            foreach (var v in loadTexQueue.Keys)
            {
                if (loadTexQueue.TryRemove(v, out var bmp))
                {
                    textures.TryAdd(v, LoadImage(bmp));
                }
            }
        }

        public static int LoadImage(SKBitmap image)
        {
            int texID = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, texID);

            using (image)
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0,
                    PixelFormat.Bgra, PixelType.UnsignedByte, image.GetPixels());

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            return texID;
        }

        public static int LoadImage(string filename)
        {
            try
            {
                return LoadImage(new Bitmap(filename));
            }
            catch (FileNotFoundException)
            {
                return -1;
            }
        }
    }
}
