﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.FSharp.Editor

open System.Collections.Generic
open Internal.Utilities.StructuredFormat
open Microsoft.CodeAnalysis
open Microsoft.FSharp.Compiler
open Microsoft.VisualStudio.Core.Imaging
open Microsoft.VisualStudio.Language.StandardClassification
open Microsoft.VisualStudio.Text.Adornments

module internal QuickInfoViewProvider =

    let layoutTagToClassificationTag (layoutTag:LayoutTag) =
        match layoutTag with
        | ActivePatternCase
        | UnionCase -> PredefinedClassificationTypeNames.SymbolDefinition
        | ActivePatternResult
        | Alias
        | Class
        | Enum
        | Interface
        | Module
        | Record
        | Struct
        | TypeParameter
        | Union
        | UnknownType
        | UnknownEntity -> PredefinedClassificationTypeNames.Type
        | Event
        | Field
        | Local
        | Method
        | Member
        | ModuleBinding
        | Namespace
        | Parameter
        | Property
        | RecordField -> PredefinedClassificationTypeNames.Identifier
        | StringLiteral -> PredefinedClassificationTypeNames.String
        | NumericLiteral -> PredefinedClassificationTypeNames.Number
        | Operator -> PredefinedClassificationTypeNames.Operator
        | Keyword -> PredefinedClassificationTypeNames.Keyword
        | LineBreak
        | Space -> PredefinedClassificationTypeNames.WhiteSpace
        | Delegate
        | Punctuation
        | Text -> PredefinedClassificationTypeNames.Other

    let provideContent
        (
            imageId:ImageId,
            description:#seq<Layout.TaggedText>,
            documentation:#seq<Layout.TaggedText>,
            typeParameterMap:#seq<Layout.TaggedText>,
            usage:#seq<Layout.TaggedText>,
            exceptions:#seq<Layout.TaggedText>,
            navigation:QuickInfoNavigation
        ) =

        let buildContainerElement (itemGroup:#seq<Layout.TaggedText>) =
            let finalCollection = List<ContainerElement>()
            let currentContainerItems = List<obj>()
            let runsCollection = List<ClassifiedTextRun>()
            let flushRuns() =
                if runsCollection.Count > 0 then
                    let element = ClassifiedTextElement(runsCollection)
                    currentContainerItems.Add(element :> obj)
                    runsCollection.Clear()
            let flushContainer() =
                if currentContainerItems.Count > 0 then
                    let element = ContainerElement(ContainerElementStyle.Wrapped, currentContainerItems)
                    finalCollection.Add(element)
                    currentContainerItems.Clear()
            for item in itemGroup do
                let classificationTag = layoutTagToClassificationTag item.Tag
                match item with
                | :? Layout.NavigableTaggedText as nav when navigation.IsTargetValid nav.Range ->
                    flushRuns()
                    let navigableTextRun = NavigableTextRun(classificationTag, item.Text, fun () -> navigation.NavigateTo nav.Range)
                    currentContainerItems.Add(navigableTextRun :> obj)
                | _ when item.Tag = LineBreak ->
                    flushRuns()
                    flushContainer()
                | _ ->
                    let newRun = ClassifiedTextRun(classificationTag, item.Text)
                    runsCollection.Add(newRun)
                ()
            flushRuns()
            flushContainer()
            finalCollection |> List.ofSeq

        let elements =
            [ description
              documentation
              typeParameterMap
              usage
              exceptions ]
            |> List.filter (Seq.isEmpty >> not)
            |> List.map buildContainerElement
            |> List.concat
            |> List.map (fun x -> x :> obj)
            |> (fun e -> ContainerElement(ContainerElementStyle.Stacked, e))
        ContainerElement(
            ContainerElementStyle.Wrapped,
            [(ImageElement(imageId) :> obj); elements :> obj])
