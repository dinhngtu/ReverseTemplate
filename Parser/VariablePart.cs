using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseTemplate.Parser {
    public abstract class VariablePart {
        public abstract string Name { get; }
    }

    public class ObjectVariablePart : VariablePart {
        public override string Name { get; }

        public ObjectVariablePart(string name) {
            Name = name;
        }
    }

    public class ArrayVariablePart : VariablePart {
        public override string Name { get; }

        public ArrayVariablePart(string name) {
            Name = name;
        }
    }
}
