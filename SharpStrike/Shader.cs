using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;

namespace SharpStrike
{
    public class Shader
    {
        private int _vsh;
        private int _fsh;

        private int _program;
        private string _shaderName;

        private Dictionary<string, int> _uniforms = new Dictionary<string, int>();

        public Shader(string shaderName, params string[] uniforms)
        {
            _shaderName = shaderName;

            LoadShader(shaderName);

            //creates and ID for this program
            _program = GL.CreateProgram();

            //attaches shaders to this program
            GL.AttachShader(_program, _vsh);
            GL.AttachShader(_program, _fsh);

            GL.LinkProgram(_program);
            GL.ValidateProgram(_program);

            RegisterUniforms(uniforms);
        }

        private void LoadShader(string shaderName)
        {
            var path = $"assets\\shaders\\{shaderName}";

            var codeVsh = File.ReadAllText(path + ".vsh");
            var codeFsh = File.ReadAllText(path + ".fsh");

            _vsh = GL.CreateShader(ShaderType.VertexShader);
            _fsh = GL.CreateShader(ShaderType.FragmentShader);

            GL.ShaderSource(_vsh, codeVsh);
            GL.ShaderSource(_fsh, codeFsh);

            GL.CompileShader(_vsh);
            GL.CompileShader(_fsh);
        }

        private int GetUniformLocation(string uniform)
        {
            if (_uniforms.TryGetValue(uniform, out var loc))
                return loc;

            Console.WriteLine($"Attempted to access unknown uniform '{uniform}' in shader '{_shaderName}'");
            return -1;
        }

        /*
        protected void BindAttributes()
        {
        }

        protected void BindAttribute(int attrib, string variable)
        {
            GL.BindAttribLocation(_program, attrib, variable);
        }*/

        private void RegisterUniforms(params string[] uniforms)
        {
            Bind();
            foreach (var uniform in uniforms)
            {
                if (_uniforms.ContainsKey(uniform))
                {
                    Console.WriteLine($"Attemted to register uniform '{uniform}' in shader '{_shaderName}' twice");
                    continue;
                }

                var loc = GL.GetUniformLocation(_program, uniform);

                if (loc == -1)
                {
                    Console.WriteLine($"Could not find uniform '{uniform}' in shader '{_shaderName}'");
                    continue;
                }

                _uniforms.Add(uniform, loc);
            }
            Unbind();
        }

        public void SetFloat(string uniform, float f)
        {
            if (_uniforms.TryGetValue(uniform, out var loc))
            {
                GL.Uniform1(loc, f);
            }
            else
            {
                Console.WriteLine($"Attempted to set unknown uniform '{uniform}' in shader '{_shaderName}'");
            }
        }

        public void SetVector2(string uniform, Vector2 vec)
        {
            var loc = GetUniformLocation(uniform);

            if (loc != -1)
                GL.Uniform2(loc, vec);
        }

        public void Bind()
        {
            GL.UseProgram(_program);
        }

        public void Unbind()
        {
            GL.UseProgram(0);
        }

        public void Destroy()
        {
            Unbind();

            GL.DetachShader(_program, _vsh);
            GL.DetachShader(_program, _fsh);

            GL.DeleteShader(_vsh);
            GL.DeleteShader(_fsh);

            GL.DeleteProgram(_program);
        }
    }
}