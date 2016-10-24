using Sigil;
using Sigil.NonGeneric;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using static System.Reflection.MethodAttributes;

namespace VkEngine
{
    public static class EmitExtensions
    {
        public static WrappedEmit<TDelegate> Wrap<TDelegate>(this Emit<TDelegate> emit)
        {
            return new WrappedEmit<TDelegate>(emit);
        }

        public static PropertyBuilder EmitProperty<TProperty>(this TypeBuilder builder, string name, MethodAttributes getAttributes = ReuseSlot, MethodAttributes setAttributes = ReuseSlot, Action<Emit<Func<TProperty>>> emitGet = null, Action<Emit<Action<TProperty>>> emitSet = null)
        {
            var property = builder.DefineProperty(name, PropertyAttributes.None, typeof(TProperty), Type.EmptyTypes);

            if (emitGet != null)
            {
                var getter = builder.EmitFunc("get_" + name, getAttributes, emitGet);

                property.SetGetMethod(getter);
            }

            if (emitSet != null)
            {
                var setter = builder.EmitAction("set_" + name, setAttributes, emitSet);

                property.SetSetMethod(setter);
            }

            return property;
        }

        public static MethodBuilder EmitOverride(this TypeBuilder builder, MethodInfo methodInfo, Action<Emit> emitAction)
        {
            var emitter = Emit.BuildMethod(methodInfo.ReturnType,
                                            methodInfo.GetParameters().Select(x => x.ParameterType).ToArray(),
                                            builder,
                                            methodInfo.Name,
                                            methodInfo.Attributes & ~(Abstract | NewSlot),
                                            CallingConventions.HasThis);

            emitAction(emitter);

            var result = emitter.CreateMethod();

            builder.DefineMethodOverride(result, methodInfo);

            return result;
        }

        public static MethodBuilder EmitOverride<TType>(this TypeBuilder builder, Expression<Func<TType, Action>> mapping, Action<Emit<Action>> emitAction)
        {
            var methodInfo = Util.GetMethodInfo(mapping);

            var emitter = Emit<Action>.BuildInstanceMethod(builder, methodInfo.Name, methodInfo.Attributes);

            emitAction(emitter);

            var result = emitter.CreateMethod();

            builder.DefineMethodOverride(result, methodInfo);

            return result;
        }

        public static MethodBuilder EmitOverride<TType, TParam>(this TypeBuilder builder, Expression<Func<TType, Action<TParam>>> mapping, Action<Emit<Action<TParam>>> emitAction)
        {
            var methodInfo = Util.GetMethodInfo(mapping);

            var emitter = Emit<Action<TParam>>.BuildInstanceMethod(builder, methodInfo.Name, methodInfo.Attributes);

            emitAction(emitter);

            var result = emitter.CreateMethod();

            builder.DefineMethodOverride(result, methodInfo);

            return result;
        }

        public static MethodBuilder EmitAction(this TypeBuilder builder, string name, MethodAttributes attributes, Action<Emit<Action>> emitAction)
        {
            var emitter = Emit<Action>.BuildInstanceMethod(builder, name, attributes);

            emitAction(emitter);

            return emitter.CreateMethod();
        }

        public static MethodBuilder EmitAction<TParam>(this TypeBuilder builder, string name, MethodAttributes attributes, Action<Emit<Action<TParam>>> emitAction)
        {
            var emitter = Emit<Action<TParam>>.BuildInstanceMethod(builder, name, attributes);

            emitAction(emitter);

            return emitter.CreateMethod();
        }

        public static MethodBuilder EmitAction<TParam1, TParam2>(this TypeBuilder builder, string name, MethodAttributes attributes, Action<Emit<Action<TParam1, TParam2>>> emitAction)
        {
            var emitter = Emit<Action<TParam1, TParam2>>.BuildInstanceMethod(builder, name, attributes);

            emitAction(emitter);

            return emitter.CreateMethod();
        }

        public static MethodBuilder EmitAction<TParam1, TParam2, TParam3>(this TypeBuilder builder, string name, MethodAttributes attributes, Action<Emit<Action<TParam1, TParam2, TParam3>>> emitAction)
        {
            var emitter = Emit<Action<TParam1, TParam2, TParam3>>.BuildInstanceMethod(builder, name, attributes);

            emitAction(emitter);

            return emitter.CreateMethod();
        }

        public static MethodBuilder EmitAction<TParam1, TParam2, TParam3, TParam4>(this TypeBuilder builder, string name, MethodAttributes attributes, Action<Emit<Action<TParam1, TParam2, TParam3, TParam4>>> emitAction)
        {
            var emitter = Emit<Action<TParam1, TParam2, TParam3, TParam4>>.BuildInstanceMethod(builder, name, attributes);

            emitAction(emitter);

            return emitter.CreateMethod();
        }

        public static MethodBuilder EmitFunc<TReturn>(this TypeBuilder builder, string name, MethodAttributes attributes, Action<Emit<Func<TReturn>>> emitAction)
        {
            var emitter = Emit<Func<TReturn>>.BuildInstanceMethod(builder, name, attributes);

            emitAction(emitter);

            return emitter.CreateMethod();
        }

        public static MethodBuilder EmitFunc<TParam, TReturn>(this TypeBuilder builder, string name, MethodAttributes attributes, Action<Emit<Func<TParam, TReturn>>> emitAction)
        {
            var emitter = Emit<Func<TParam, TReturn>>.BuildInstanceMethod(builder, name, attributes);

            emitAction(emitter);

            return emitter.CreateMethod();
        }

        public static MethodBuilder EmitFunc<TParam1, TParam2, TReturn>(this TypeBuilder builder, string name, MethodAttributes attributes, Action<Emit<Func<TParam1, TParam2, TReturn>>> emitAction)
        {
            var emitter = Emit<Func<TParam1, TParam2, TReturn>>.BuildInstanceMethod(builder, name, attributes);

            emitAction(emitter);

            return emitter.CreateMethod();
        }

        public static MethodBuilder EmitFunc<TParam1, TParam2, TParam3, TReturn>(this TypeBuilder builder, string name, MethodAttributes attributes, Action<Emit<Func<TParam1, TParam2, TParam3, TReturn>>> emitAction)
        {
            var emitter = Emit<Func<TParam1, TParam2, TParam3, TReturn>>.BuildInstanceMethod(builder, name, attributes);

            emitAction(emitter);

            return emitter.CreateMethod();
        }

        public static MethodBuilder EmitFunc<TParam1, TParam2, TParam3, TParam4, TReturn>(this TypeBuilder builder, string name, MethodAttributes attributes, Action<Emit<Func<TParam1, TParam2, TParam3, TParam4, TReturn>>> emitAction)
        {
            var emitter = Emit<Func<TParam1, TParam2, TParam3, TParam4, TReturn>>.BuildInstanceMethod(builder, name, attributes);

            emitAction(emitter);

            return emitter.CreateMethod();
        }
    }

    internal static class Util
    {

        internal static MethodInfo GetMethodInfo(LambdaExpression mapping)
        {
            return (MethodInfo)((ConstantExpression)((MethodCallExpression)((UnaryExpression)mapping.Body).Operand).Object).Value;
        }
    }

    public class WrappedEmit<TDelegate>
    {
        private Emit<TDelegate> emit;

        public FromEmit<TDelegate, TType> From<TType>()
        {
            return new FromEmit<TDelegate, TType>(this.emit);
        }

        public WrappedEmit(Emit<TDelegate> emit)
        {
            this.emit = emit;
        }

        private static FieldInfo GetFieldInfo(LambdaExpression mapping)
        {
            return (FieldInfo)((MemberExpression)mapping.Body).Member;
        }

        public Emit<TDelegate> Load<TField>(Expression<Func<TField>> mapping)
        {
            this.emit.LoadField(GetFieldInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> Call(Expression<Func<Action>> mapping)
        {
            this.emit.Call(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> Call<TParam>(Expression<Func<Action<TParam>>> mapping)
        {
            this.emit.Call(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> Call<TParam1, TParam2>(Expression<Func<Action<TParam1, TParam2>>> mapping)
        {
            this.emit.Call(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> Call<TParam1, TParam2, TParam3>(Expression<Func<Action<TParam1, TParam2, TParam3>>> mapping)
        {
            this.emit.Call(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> Call<TParam1, TParam2, TParam3, TParam4>(Expression<Func<Action<TParam1, TParam2, TParam3, TParam4>>> mapping)
        {
            this.emit.Call(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> Call<TReturn>(Expression<Func<Func<TReturn>>> mapping)
        {
            this.emit.Call(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> Call<TParam, TReturn>(Expression<Func<Func<TParam, TReturn>>> mapping)
        {
            this.emit.Call(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> Call<TParam1, TParam2, TReturn>(Expression<Func<Func<TParam1, TParam2, TReturn>>> mapping)
        {
            this.emit.Call(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> Call<TParam1, TParam2, TParam3, TReturn>(Expression<Func<Func<TParam1, TParam2, TParam3, TReturn>>> mapping)
        {
            this.emit.Call(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> Call<TParam1, TParam2, TParam3, TParam4, TReturn>(Expression<Func<Func<TParam1, TParam2, TParam3, TParam4, TReturn>>> mapping)
        {
            this.emit.Call(Util.GetMethodInfo(mapping));

            return this.emit;
        }
    }

    public class FromEmit<TDelegate, TType>
    {
        private Emit<TDelegate> emit;

        public FromEmit(Emit<TDelegate> emit)
        {
            this.emit = emit;
        }

        private static MethodInfo GetMethodInfo(LambdaExpression mapping)
        {
            return (MethodInfo)((ConstantExpression)((MethodCallExpression)((UnaryExpression)mapping.Body).Operand).Object).Value;
        }

        private static FieldInfo GetFieldInfo(LambdaExpression mapping)
        {
            return (FieldInfo)((MemberExpression)mapping.Body).Member;
        }

        public Emit<TDelegate> Get<TProperty>(Expression<Func<TType, TProperty>> mapping)
        {
            var propertyInfo = (PropertyInfo)((MemberExpression)mapping.Body).Member;

            this.emit.CallVirtual(propertyInfo.GetGetMethod(true));

            return this.emit;
        }

        public Emit<TDelegate> Set<TProperty>(Expression<Func<TType, TProperty>> mapping)
        {
            var propertyInfo = (PropertyInfo)((MemberExpression)mapping.Body).Member;

            this.emit.CallVirtual(propertyInfo.GetSetMethod(true));

            return this.emit;
        }

        public Emit<TDelegate> Load<TField>(Expression<Func<TType, TField>> mapping)
        {
            this.emit.LoadField(GetFieldInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> Store<TField>(Expression<Func<TType, TField>> mapping)
        {
            this.emit.StoreField(GetFieldInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> CallVirtual(Expression<Func<TType, Action>> mapping)
        {
            this.emit.CallVirtual(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> CallVirtual<TParam>(Expression<Func<TType, Action<TParam>>> mapping)
        {
            this.emit.CallVirtual(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> CallVirtual<TParam1, TParam2>(Expression<Func<TType, Action<TParam1, TParam2>>> mapping)
        {
            this.emit.CallVirtual(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> CallVirtual<TParam1, TParam2, TParam3>(Expression<Func<TType, Action<TParam1, TParam2, TParam3>>> mapping)
        {
            this.emit.CallVirtual(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> CallVirtual<TParam1, TParam2, TParam3, TParam4>(Expression<Func<TType, Action<TParam1, TParam2, TParam3, TParam4>>> mapping)
        {
            this.emit.CallVirtual(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> CallVirtual<TReturn>(Expression<Func<TType, Func<TReturn>>> mapping)
        {
            this.emit.CallVirtual(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> CallVirtual<TReturn, TParam>(Expression<Func<TType, Func<TParam, TReturn>>> mapping)
        {
            this.emit.CallVirtual(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> CallVirtual<TReturn, TParam1, TParam2>(Expression<Func<TType, Func<TParam1, TParam2, TReturn>>> mapping)
        {
            this.emit.CallVirtual(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> CallVirtual<TReturn, TParam1, TParam2, TParam3>(Expression<Func<TType, Func<TParam1, TParam2, TParam3, TReturn>>> mapping)
        {
            this.emit.CallVirtual(Util.GetMethodInfo(mapping));

            return this.emit;
        }

        public Emit<TDelegate> CallVirtual<TReturn, TParam1, TParam2, TParam3, TParam4>(Expression<Func<TType, Func<TParam1, TParam2, TParam3, TParam4, TReturn>>> mapping)
        {
            this.emit.CallVirtual(Util.GetMethodInfo(mapping));

            return this.emit;
        }
    }
}
