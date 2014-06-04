﻿namespace TopHat.Tests.CodeGeneration.Fixtures {
    using Moq;

    using TopHat.CodeGeneration;
    using TopHat.Configuration;
    using TopHat.Engine;
    using TopHat.Tests.TestDomain;

    public class GenerateCodeFixture {
        private readonly Mock<IEngine> engine = new Mock<IEngine>();

        public IGeneratedCodeManager CodeManager { get; private set; }

        public GenerateCodeFixture() {
            // generate config and assembly
            IConfiguration config = new CustomConfig(this.engine.Object);
            var codeGenerator = new CodeGenerator();
            var codeConfig = new CodeGeneratorConfig();
            codeConfig.GenerateAssembly = true;
            codeConfig.GenerateSource = true;
            codeGenerator.Generate(config, codeConfig);
            this.CodeManager = new GeneratedCodeManager(codeConfig);
            this.CodeManager.LoadCode();
        }

        private class CustomConfig : DefaultConfiguration {
            public CustomConfig(IEngine engine)
                : base(engine, string.Empty) {
                this.AddNamespaceOf<Post>();
            }
        }
    }
}