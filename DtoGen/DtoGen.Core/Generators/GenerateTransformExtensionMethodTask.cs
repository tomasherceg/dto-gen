﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DtoGen.Core.Generators
{
    /// <summary>
    /// Generates the TransformToTarget and PopulateTarget extension methods that transform source object to the target object.
    /// </summary>
    public class GenerateTransformExtensionMethodTask : TransformTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateTransformExtensionMethodTask"/> class.
        /// </summary>
        public GenerateTransformExtensionMethodTask(Transform transform) : base(transform)
        {
        }

        /// <summary>
        /// Renders the code
        /// </summary>
        public override IEnumerable<MemberDeclarationSyntax> Render()
        {
            // TransformToTarget method
            var method = SyntaxHelper.GenerateExtensionMethod("TransformToTarget", Transform.TargetType.FullName, new[]
                {
                    SyntaxHelper.GenerateMethodParameter("source", Transform.SourceType.FullName, true)
                },
                new[] { 
                    SyntaxHelper.GenerateAttribute(
                        typeof(DtoConvertFunctionAttribute),
                        SyntaxFactory.TypeOfExpression(SyntaxHelper.GenerateTypeSyntax(Transform.SourceType)), 
                        SyntaxFactory.TypeOfExpression(SyntaxHelper.GenerateTypeSyntax(Transform.TargetType)),
                        SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                    )
                })
                .WithBody(SyntaxFactory.Block(
                    new StatementSyntax[] {
                        SyntaxHelper.GenerateStaticMethodCall("EnsureInitialized", typeof (PropertyConverter).FullName),
                        SyntaxHelper.GenerateVariableDeclarationAndObjectCreationStatement("target", Transform.TargetType.FullName),
                    }.Concat(
                        Transform.Members.SelectMany(m => m.PropertyMemberRenderer.GetTransformCode()).ToArray()
                    ).Concat(new[] {
                        SyntaxFactory.ReturnStatement(SyntaxFactory.ParseName("target"))
                    })
                ));

            // PopulateTarget method
            var method2 = SyntaxHelper.GenerateExtensionMethod("PopulateTarget", null, new[]
                {
                    SyntaxHelper.GenerateMethodParameter("source", Transform.SourceType.FullName, true),
                    SyntaxHelper.GenerateMethodParameter("target", Transform.TargetType.FullName, false)
                },
                new[] { 
                    SyntaxHelper.GenerateAttribute(
                        typeof(DtoConvertFunctionAttribute),
                        SyntaxFactory.TypeOfExpression(SyntaxHelper.GenerateTypeSyntax(Transform.SourceType)), 
                        SyntaxFactory.TypeOfExpression(SyntaxHelper.GenerateTypeSyntax(Transform.TargetType)),
                        SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)
                    )
                })
                .WithBody(SyntaxFactory.Block(
                    new StatementSyntax[] {
                        SyntaxHelper.GenerateStaticMethodCall("EnsureInitialized", typeof(PropertyConverter).FullName),
                    }.Concat(
                        Transform.Members.SelectMany(m => m.PropertyMemberRenderer.GetTransformCode()).ToArray()
                    )
                ));

            // generate the static class
            var className = Transform.SourceType.Name + "Extensions";
            yield return SyntaxHelper.GenerateNamespace(Transform.SourceType.Namespace, new MemberDeclarationSyntax[]
            {
                SyntaxHelper.GenerateClass(
                    className, 
                    new[] { SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword }, 
                    new MemberDeclarationSyntax[] { method, method2 },
                    new[] { SyntaxHelper.GenerateAttribute(typeof(DtoGeneratedAttribute)) }
                ) 
            },
            GetUsingsForNamespace());
        }
    }
}