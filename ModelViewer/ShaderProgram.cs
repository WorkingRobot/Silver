using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ModelViewer.Properties;
using OpenTK.Graphics.OpenGL4;

namespace Silver.ModelViewer
{
    class ShaderProgram
    {
        public int ProgramID = -1;
        public int VShaderID = -1;
        public int FShaderID = -1;
        public int AttributeCount = 0;
        public int UniformCount = 0;

        public Dictionary<string, AttributeInfo> Attributes = new Dictionary<string, AttributeInfo>();
        public Dictionary<string, UniformInfo> Uniforms = new Dictionary<string, UniformInfo>();
        public Dictionary<string, uint> Buffers = new Dictionary<string, uint>();

        public ShaderProgram()
        {
            ProgramID = GL.CreateProgram();
        }

        private void LoadShader(string code, ShaderType type, out int address)
        {
            address = GL.CreateShader(type);
            GL.ShaderSource(address, code);
            GL.CompileShader(address);
            GL.AttachShader(ProgramID, address);
            var log = GL.GetShaderInfoLog(address);
            if (!string.IsNullOrWhiteSpace(log))
                Console.WriteLine(log);
        }

        public void LoadShaderFromString(string code, ShaderType type)
        {
            if (type == ShaderType.VertexShader)
            {
                LoadShader(code, type, out VShaderID);
            }
            else if (type == ShaderType.FragmentShader)
            {
                LoadShader(code, type, out FShaderID);
            }
        }

        public void LoadShaderFromFile(string filename, ShaderType type)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                if (type == ShaderType.VertexShader)
                {
                    LoadShader(sr.ReadToEnd(), type, out VShaderID);
                }
                else if (type == ShaderType.FragmentShader)
                {
                    LoadShader(sr.ReadToEnd(), type, out FShaderID);
                }
            }
        }

        public void Link()
        {
            GL.LinkProgram(ProgramID);
            var log = GL.GetProgramInfoLog(ProgramID);
            if (!string.IsNullOrWhiteSpace(log))
                Console.WriteLine(log);
            GL.GetProgram(ProgramID, GetProgramParameterName.ActiveAttributes, out AttributeCount);
            GL.GetProgram(ProgramID, GetProgramParameterName.ActiveUniforms, out UniformCount);

            for (int i = 0; i < AttributeCount; i++)
            {
                AttributeInfo info = new AttributeInfo();
                GL.GetActiveAttrib(ProgramID, i, 256, out var length, out info.size, out info.type, out info.name);
                info.address = GL.GetAttribLocation(ProgramID, info.name);
                Attributes.Add(info.name, info);
            }

            for (int i = 0; i < UniformCount; i++)
            {
                UniformInfo info = new UniformInfo();
                GL.GetActiveUniform(ProgramID, i, 256, out var length, out info.size, out info.type, out info.name);
                Uniforms.Add(info.name, info);
                info.address = GL.GetUniformLocation(ProgramID, info.name);
            }
        }

        public void GenBuffers()
        {
            for (int i = 0; i < Attributes.Count; i++)
            {
                GL.GenBuffers(1, out uint buffer);
                Buffers.Add(Attributes.Values.ElementAt(i).name, buffer);
            }

            for (int i = 0; i < Uniforms.Count; i++)
            {
                GL.GenBuffers(1, out uint buffer);
                Buffers.Add(Uniforms.Values.ElementAt(i).name, buffer);
            }
        }

        public void EnableVertexAttribArrays()
        {
            for (int i = 0; i < Attributes.Count; i++)
            {
                GL.EnableVertexAttribArray(Attributes.Values.ElementAt(i).address);
            }
        }

        public void DisableVertexAttribArrays()
        {
            for (int i = 0; i < Attributes.Count; i++)
            {
                GL.DisableVertexAttribArray(Attributes.Values.ElementAt(i).address);
            }
        }

        public int GetAttribute(string name) => Attributes.ContainsKey(name) ? Attributes[name].address : -1;

        public int GetUniform(string name) => Uniforms.ContainsKey(name) ? Uniforms[name].address : -1;

        public uint GetBuffer(string name) => Buffers.ContainsKey(name) ? Buffers[name] : 0;

        public ShaderProgram(byte[] vshader, byte[] fshader)
        {
            ProgramID = GL.CreateProgram();
            LoadShader(Encoding.UTF8.GetString(vshader), ShaderType.VertexShader, out VShaderID);
            LoadShader(Encoding.UTF8.GetString(fshader), ShaderType.FragmentShader, out FShaderID);

            Link();
            GenBuffers();
        }

        public ShaderProgram(string vshader, string fshader, bool fromFile = false)
        {
            ProgramID = GL.CreateProgram();

            if (fromFile)
            {
                LoadShaderFromFile(vshader, ShaderType.VertexShader);
                LoadShaderFromFile(fshader, ShaderType.FragmentShader);
            }
            else
            {
                LoadShaderFromString(vshader, ShaderType.VertexShader);
                LoadShaderFromString(fshader, ShaderType.FragmentShader);
            }

            Link();
            GenBuffers();
        }

        public class UniformInfo
        {
            public string name = "";
            public int address = -1;
            public int size = 0;
            public ActiveUniformType type;
        }

        public class AttributeInfo
        {
            public string name = "";
            public int address = -1;
            public int size = 0;
            public ActiveAttribType type;
        }
    }
}
