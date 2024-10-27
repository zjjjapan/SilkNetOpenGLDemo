using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace SilkNetOpenGLDemo
{
    /// <summary>
    /// OpenGLLineControl.xaml 的交互逻辑
    /// </summary>
    public partial class OpenGLLineControl : UserControl
    {
        private IWindow _glWindow;
        private GL _gl;
        private uint _vao;
        private uint _vbo;
        private Shader _shader;
        private WriteableBitmap _bitmap;
        private int _width = 800;
        private int _height = 450;

        // 初始线段顶点坐标
        private float[] vertices = {
            100f, 100f, // 起点
            300f, 300f  // 终点
        };

        public OpenGLLineControl()
        {
            InitializeComponent();
            Loaded += OpenGLLineControl_Loaded;
        }

        private void OpenGLLineControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeOpenGL();
        }

        private void InitializeOpenGL()
        {
            var options = WindowOptions.Default;
            options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 3));
            options.Size = new Vector2D<int>(_width, _height);
            options.IsVisible = false;

            _glWindow = Silk.NET.Windowing.Window.Create(options);
            _glWindow.Load += OnLoad;
            _glWindow.Render += OnRender;
            _glWindow.Closing += OnClose;
            _glWindow.Initialize();
        }

        private void OnLoad()
        {
            _gl = GL.GetApi(_glWindow);

            // 设置 VAO 和 VBO
            _vao = _gl.GenVertexArray();
            _vbo = _gl.GenBuffer();

            UpdateVertexData();

            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), IntPtr.Zero);
            _gl.EnableVertexAttribArray(0);

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _gl.BindVertexArray(0);

            // 加载并设置着色器
            _shader = new Shader(_gl, "VertexShader.glsl", "FragmentShader.glsl");

            // 创建正交投影矩阵
            Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(0, _width, 0, _height, -1, 1);
            _shader.Use();
            _shader.SetMatrix4("projection", projection);

            // 初始化位图
            _bitmap = new WriteableBitmap(_width, _height, 96, 96, PixelFormats.Bgr32, null);
            OpenGLImage.Source = _bitmap;
        }

        private void OnRender(double deltaTime)
        {
            _gl.Clear((uint)ClearBufferMask.ColorBufferBit);

            _shader.Use();

            _gl.BindVertexArray(_vao);
            _gl.DrawArrays(PrimitiveType.Lines, 0, 2);
            _gl.BindVertexArray(0);

            RenderToBitmap();
        }

        private void RenderToBitmap()
        {
            int bufferSize = _width * _height * 4;
            byte[] buffer = new byte[bufferSize];

            unsafe
            {
                fixed (byte* ptr = buffer)
                {
                    _gl.ReadPixels(0, 0, (uint)_width, (uint)_height, GLEnum.Bgra, GLEnum.UnsignedByte, ptr);
                }
            }

            _bitmap.Lock();
            _bitmap.WritePixels(new Int32Rect(0, 0, _width, _height), buffer, _width * 4, 0);
            _bitmap.Unlock();
        }

        private void UpdateVertexData()
        {
            _gl.BindVertexArray(_vao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), ref vertices[0], GLEnum.DynamicDraw);
        }

        public void MoveLine(float xOffset, float yOffset)
        {
            vertices[0] += xOffset; // 起点 X 偏移
            vertices[1] += yOffset; // 起点 Y 偏移
            vertices[2] += xOffset; // 终点 X 偏移
            vertices[3] += yOffset; // 终点 Y 偏移

            UpdateVertexData();
            _glWindow.DoRender(); // 手动触发渲染
        }

        private void OnClose()
        {
            _gl.DeleteVertexArray(_vao);
            _gl.DeleteBuffer(_vbo);
            _shader.Dispose();
            _glWindow?.Dispose();
        }
    }

    // 着色器类实现
    public class Shader
    {
        private GL _gl;
        private uint _program;

        public Shader(GL gl, string vertexPath, string fragmentPath)
        {
            _gl = gl;

            string vertexShaderSource = File.ReadAllText(vertexPath);
            string fragmentShaderSource = File.ReadAllText(fragmentPath);

            uint vertexShader = CompileShader(ShaderType.VertexShader, vertexShaderSource);
            uint fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentShaderSource);

            _program = _gl.CreateProgram();
            _gl.AttachShader(_program, vertexShader);
            _gl.AttachShader(_program, fragmentShader);
            _gl.LinkProgram(_program);

            _gl.GetProgram(_program, GLEnum.LinkStatus, out int status);
            if (status == 0)
            {
                throw new Exception($"Program linking failed: {_gl.GetProgramInfoLog(_program)}");
            }

            _gl.DetachShader(_program, vertexShader);
            _gl.DetachShader(_program, fragmentShader);
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
        }

        public void Use()
        {
            _gl.UseProgram(_program);
        }

        public void SetMatrix4(string name, Matrix4x4 matrix)
        {
            int location = _gl.GetUniformLocation(_program, name);
            if (location == -1)
            {
                Console.WriteLine($"Warning: uniform '{name}' not found in shader.");
            }
            else
            {
                unsafe
                {
                    _gl.UniformMatrix4(location, 1, false, (float*)&matrix);
                }
            }
        }

        private uint CompileShader(ShaderType type, string source)
        {
            uint shader = _gl.CreateShader(type);
            _gl.ShaderSource(shader, source);
            _gl.CompileShader(shader);

            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
            if (status == 0)
            {
                string infoLog = _gl.GetShaderInfoLog(shader);
                throw new Exception($"Error compiling shader ({type}): {infoLog}");
            }

            return shader;
        }

        public void Dispose()
        {
            _gl.DeleteProgram(_program);
        }
    }
}
